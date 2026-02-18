namespace Djambi.Api.Logic.Managers

open Djambi.Api.Db.Interfaces
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Logic.Services
open Djambi.Api.Model
open System.Threading.Tasks

type TurnManager(eventRepo : IEventRepository,
                 eventServ : EventService,
                 gameRepo : IGameRepository,
                 notificationServ : INotificationService,
                 selectionServ : SelectionService,
                 turnServ : TurnService) =

    interface ITurnManager with
        member x.selectCell (gameId, cellId) session =
            EventProcessing.processEvent eventRepo eventServ gameRepo notificationServ
                gameId (fun game ->
                    selectionServ.getCellSelectedEvent (game, cellId) session
                )

        member x.resetTurn gameId session =
            EventProcessing.processEvent eventRepo eventServ gameRepo notificationServ
                gameId (fun game ->
                    turnServ.getResetTurnEvent game session
                )

        member x.commitTurn gameId session =
            EventProcessing.processEvent eventRepo eventServ gameRepo notificationServ
                gameId (fun game ->
                    turnServ.getCommitTurnEvent game session
                )

        member x.skipTurn gameId session =
            EventProcessing.processEvent eventRepo eventServ gameRepo notificationServ
                gameId (fun game ->
                    turnServ.getSkipTurnEvent game session
                )
