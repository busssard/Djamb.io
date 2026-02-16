namespace Djambi.Api.Host

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Db.Interfaces
open Djambi.Api.Model
open Djambi.Api.Enums

type BotRunner(scopeFactory : IServiceScopeFactory,
               botRegistry : IBotRegistry,
               logger : ILogger<BotRunner>) =

    let mutable timer : Timer option = None

    let createBotSession (game : Game) : Session =
        {
            id = 0
            user =
                {
                    id = game.createdBy.userId
                    name = "BotRunner"
                    email = None
                    privileges = [Privilege.OpenParticipation; Privilege.ViewGames]
                }
            token = "bot-internal"
            createdOn = DateTime.UtcNow
            expiresOn = DateTime.UtcNow.AddHours(24.0)
        }

    let tryPlayBotTurn () : Task =
        task {
            try
                use scope = scopeFactory.CreateScope()
                let searchRepo = scope.ServiceProvider.GetRequiredService<ISearchRepository>()
                let gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>()
                let turnManager = scope.ServiceProvider.GetRequiredService<ITurnManager>()

                // Search for InProgress games
                let query : GamesQuery =
                    { GamesQuery.empty with
                        statuses = [GameStatus.InProgress]
                    }
                // Use userId=0 since bot doesn't need containsMe filter
                let! searchResults = searchRepo.searchGames (query, 0)

                for sg in searchResults do
                    try
                        let! game = gameRepo.getGame sg.id

                        if game.status = GameStatus.InProgress && game.turnCycle.Length > 0 then
                            let currentPlayerId = game.turnCycle.Head
                            let currentPlayer = game.players |> List.tryFind (fun p -> p.id = currentPlayerId)

                            match currentPlayer with
                            | Some player when player.kind = PlayerKind.Neutral ->
                                // It's a Neutral player's turn - bot should play
                                let bot =
                                    match botRegistry.getBot "minimax" with
                                    | Some b -> b
                                    | None ->
                                        match botRegistry.getBot "random" with
                                        | Some b -> b
                                        | None -> failwith "No bot available"

                                let session = createBotSession game

                                logger.LogDebug("Bot playing for Neutral player {PlayerId} in game {GameId}", currentPlayerId, game.id)

                                // Selection loop
                                let mutable currentGame = game
                                let mutable continueSelecting = true

                                while continueSelecting do
                                    match currentGame.currentTurn with
                                    | Some turn when turn.status = TurnStatus.AwaitingSelection && not turn.selectionOptions.IsEmpty ->
                                        let! cellId = bot.selectCell currentGame currentPlayerId
                                        let! response = turnManager.selectCell (currentGame.id, cellId) session
                                        currentGame <- response.game
                                    | Some turn when turn.status = TurnStatus.AwaitingCommit ->
                                        let! response = turnManager.commitTurn currentGame.id session
                                        currentGame <- response.game
                                        continueSelecting <- false
                                    | _ ->
                                        // DeadEnd or unexpected state - reset and skip
                                        continueSelecting <- false

                                // Short delay between turns for UX
                                do! Task.Delay(1500)
                            | _ -> ()
                    with
                    | ex ->
                        logger.LogWarning(ex, "Bot failed to play turn in game {GameId}", sg.id)
            with
            | ex ->
                logger.LogError(ex, "BotRunner polling error")
        } :> Task

    let onTimerTick (_ : obj) =
        // Fire and forget - exceptions are caught inside tryPlayBotTurn
        tryPlayBotTurn () |> ignore

    interface IHostedService with
        member _.StartAsync(_ct : CancellationToken) =
            logger.LogInformation("BotRunner starting - polling every 2 seconds for Neutral player turns")
            timer <- Some (new Timer(TimerCallback(onTimerTick), null, TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(2.0)))
            Task.CompletedTask

        member _.StopAsync(_ct : CancellationToken) =
            logger.LogInformation("BotRunner stopping")
            match timer with
            | Some t ->
                t.Change(Timeout.Infinite, 0) |> ignore
                t.Dispose()
                timer <- None
            | None -> ()
            Task.CompletedTask
