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
            EventProcessing.processEvent eventRepo eventServ gameRepo notificationServ
                gameId (fun game ->
                    playerServ.getAddPlayerEvent (game, request) session
                )

        member x.removePlayer (gameId, playerId) session =
            EventProcessing.processEvent eventRepo eventServ gameRepo notificationServ
                gameId (fun game ->
                    playerServ.getRemovePlayerEvent (game, playerId) session
                )

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
