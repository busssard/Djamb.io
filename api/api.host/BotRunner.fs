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
    let mutable cancellationToken = CancellationToken.None
    let executionLock = new SemaphoreSlim(1, 1)

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

    let tryPlayBotTurn (ct : CancellationToken) : Task =
        task {
            try
                use scope = scopeFactory.CreateScope()
                let searchRepo = scope.ServiceProvider.GetRequiredService<ISearchRepository>()
                let gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>()
                let turnManager = scope.ServiceProvider.GetRequiredService<ITurnManager>()

                let query : GamesQuery =
                    { GamesQuery.empty with
                        statuses = [GameStatus.InProgress]
                    }
                let! searchResults = searchRepo.searchGames (query, 0)

                for sg in searchResults do
                    if ct.IsCancellationRequested then () else
                    try
                        let! game = gameRepo.getGame sg.id

                        if game.status = GameStatus.InProgress && game.turnCycle.Length > 0 then
                            let currentPlayerId = game.turnCycle.Head
                            let currentPlayer = game.players |> List.tryFind (fun p -> p.id = currentPlayerId)

                            match currentPlayer with
                            | Some player when player.kind = PlayerKind.Neutral ->
                                let bot =
                                    match botRegistry.getBot "minimax" with
                                    | Some b -> b
                                    | None ->
                                        match botRegistry.getBot "random" with
                                        | Some b -> b
                                        | None -> failwith "No bot available"

                                let session = createBotSession game

                                logger.LogDebug("Bot playing for Neutral player {PlayerId} in game {GameId}", currentPlayerId, game.id)

                                let mutable currentGame = game
                                let mutable continueSelecting = true

                                while continueSelecting && not ct.IsCancellationRequested do
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
                                        continueSelecting <- false

                                do! Task.Delay(1500, ct)
                            | _ -> ()
                    with
                    | :? OperationCanceledException -> ()
                    | ex ->
                        logger.LogWarning(ex, "Bot failed to play turn in game {GameId}", sg.id)
            with
            | :? OperationCanceledException -> ()
            | ex ->
                logger.LogError(ex, "BotRunner polling error")
        } :> Task

    let onTimerTick (_ : obj) =
        if executionLock.Wait(0) then
            task {
                try
                    do! tryPlayBotTurn cancellationToken
                finally
                    executionLock.Release() |> ignore
            } |> ignore

    interface IHostedService with
        member _.StartAsync(ct : CancellationToken) =
            cancellationToken <- ct
            logger.LogInformation("BotRunner starting - polling every 5 seconds for Neutral player turns")
            timer <- Some (new Timer(TimerCallback(onTimerTick), null, TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0)))
            Task.CompletedTask

        member _.StopAsync(_ct : CancellationToken) =
            logger.LogInformation("BotRunner stopping")
            match timer with
            | Some t ->
                t.Change(Timeout.Infinite, 0) |> ignore
                executionLock.Wait() |> ignore
                executionLock.Release() |> ignore
                t.Dispose()
                timer <- None
            | None -> ()
            Task.CompletedTask

    interface IDisposable with
        member _.Dispose() =
            executionLock.Dispose()
            match timer with
            | Some t -> t.Dispose()
            | None -> ()
