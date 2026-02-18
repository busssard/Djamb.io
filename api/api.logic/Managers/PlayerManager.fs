namespace Djambi.Api.Logic.Managers

open Djambi.Api.Db.Interfaces
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Logic.Services
open Djambi.Api.Model
open Djambi.Api.Enums
open System.Threading.Tasks

type PlayerManager(eventRepo : IEventRepository,
                   eventServ : EventService,
                   gameRepo : IGameRepository,
                   notificationServ : INotificationService,
                   playerServ : PlayerService,
                   playerStatusChangeServ : PlayerStatusChangeService) =

    interface IPlayerManager with
        member x.addPlayer gameId request session =
            task {
                let! response =
                    EventProcessing.processEvent eventRepo eventServ gameRepo notificationServ
                        gameId (fun game ->
                            playerServ.getAddPlayerEvent (game, request) session
                        )

                // If a bot was added, save the bot assignment with the real player ID
                match request.botName with
                | Some botName ->
                    let addedPlayer =
                        response.game.players
                        |> List.tryFind (fun p ->
                            p.kind = PlayerKind.Neutral
                            && p.name = request.name.Value
                            && not (response.game.botAssignments |> Map.containsKey p.id))
                    match addedPlayer with
                    | Some p ->
                        let assignments = response.game.botAssignments |> Map.add p.id botName
                        let updatedGame = { response.game with botAssignments = assignments }
                        do! gameRepo.updateGame(updatedGame, commit=true)
                        return { response with game = updatedGame }
                    | None -> return response
                | None -> return response
            }

        member x.removePlayer (gameId, playerId) session =
            task {
                let! response =
                    EventProcessing.processEvent eventRepo eventServ gameRepo notificationServ
                        gameId (fun game ->
                            playerServ.getRemovePlayerEvent (game, playerId) session
                        )

                // If a bot player was removed, clean up bot assignment
                if response.game.botAssignments |> Map.containsKey playerId then
                    let assignments = response.game.botAssignments |> Map.remove playerId
                    let updatedGame = { response.game with botAssignments = assignments }
                    do! gameRepo.updateGame(updatedGame, commit=true)
                    return { response with game = updatedGame }
                else
                    return response
            }

        member x.updatePlayerStatus (gameId, playerId, status) session =
            let request =
                {
                    gameId = gameId
                    playerId = playerId
                    status = status
                }
            EventProcessing.processEvent eventRepo eventServ gameRepo notificationServ
                request.gameId (fun game ->
                    playerStatusChangeServ.getUpdatePlayerStatusEvent (game, request) session
                )
