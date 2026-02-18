namespace Djambi.Api.Host

open System
open System.Linq
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Db.Interfaces
open Djambi.Api.Db.Model
open Djambi.Api.Model
open Djambi.Api.Enums

type TurnTimeoutRunner(scopeFactory : IServiceScopeFactory,
                       logger : ILogger<TurnTimeoutRunner>) =

    let mutable timer : Timer option = None
    let cts = new CancellationTokenSource()
    let executionLock = new SemaphoreSlim(1, 1)

    let createSystemSession (game : Game) : Session =
        {
            id = 0
            user =
                {
                    id = game.createdBy.userId
                    name = "TurnTimer"
                    email = None
                    privileges = [Privilege.OpenParticipation; Privilege.ViewGames]
                }
            token = "timer-internal"
            createdOn = DateTime.UtcNow
            expiresOn = DateTime.UtcNow.AddHours(24.0)
        }

    let trySkipTimedOutTurns (ct : CancellationToken) : Task =
        task {
            try
                use scope = scopeFactory.CreateScope()
                let dbContext = scope.ServiceProvider.GetRequiredService<DjambiDbContext>()
                let gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>()
                let turnManager = scope.ServiceProvider.GetRequiredService<ITurnManager>()

                let! gameIds =
                    dbContext.Games
                        .Where(fun g ->
                            g.GameStatusId = GameStatus.InProgress
                            && g.TurnTimeLimitSeconds.HasValue)
                        .Select(fun g -> g.GameId)
                        .ToListAsync(ct)

                for gameId in gameIds do
                    if ct.IsCancellationRequested then () else
                    try
                        let! game = gameRepo.getGame gameId

                        if game.status = GameStatus.InProgress && game.turnCycle.Length > 0 then
                            match game.parameters.turnTimeLimitSeconds, game.currentTurn with
                            | Some limitSeconds, Some turn ->
                                match turn.turnStartedAt with
                                | Some startedAt ->
                                    let elapsed = DateTime.UtcNow - startedAt
                                    if elapsed > TimeSpan.FromSeconds(float limitSeconds) then
                                        let currentPlayerId = game.turnCycle.Head
                                        let session = createSystemSession game
                                        logger.LogInformation(
                                            "Turn timed out for player {PlayerId} in game {GameId} (elapsed: {Elapsed}s, limit: {Limit}s)",
                                            currentPlayerId, game.id, int elapsed.TotalSeconds, limitSeconds)
                                        let! _ = turnManager.skipTurn game.id session
                                        ()
                                | None -> ()
                            | _ -> ()
                    with
                    | :? OperationCanceledException -> ()
                    | ex ->
                        logger.LogWarning(ex, "TurnTimeoutRunner failed to skip turn in game {GameId}", gameId)
            with
            | :? OperationCanceledException -> ()
            | ex ->
                logger.LogError(ex, "TurnTimeoutRunner polling error")
        } :> Task

    let onTimerTick (_ : obj) =
        if executionLock.Wait(0) then
            task {
                try
                    do! trySkipTimedOutTurns cts.Token
                finally
                    executionLock.Release() |> ignore
            } |> ignore

    interface IHostedService with
        member _.StartAsync(_ct : CancellationToken) =
            logger.LogInformation("TurnTimeoutRunner starting - polling every 10 seconds for timed-out turns")
            timer <- Some (new Timer(TimerCallback(onTimerTick), null, TimeSpan.FromSeconds(10.0), TimeSpan.FromSeconds(10.0)))
            Task.CompletedTask

        member _.StopAsync(_ct : CancellationToken) =
            logger.LogInformation("TurnTimeoutRunner stopping")
            cts.Cancel()
            match timer with
            | Some t ->
                t.Change(Timeout.Infinite, 0) |> ignore
                if executionLock.Wait(TimeSpan.FromSeconds(5.0)) then
                    executionLock.Release() |> ignore
                t.Dispose()
                timer <- None
            | None -> ()
            Task.CompletedTask

    interface IDisposable with
        member _.Dispose() =
            cts.Dispose()
            executionLock.Dispose()
            match timer with
            | Some t -> t.Dispose()
            | None -> ()
