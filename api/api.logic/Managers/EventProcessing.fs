module Djambi.Api.Logic.Managers.EventProcessing

open System
open System.Threading.Tasks
open Djambi.Api.Db.Interfaces
open Djambi.Api.Logic.Services
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Model
open Djambi.Api.Enums

let isPublishable (event : Event) : bool =
    match event.kind with
    | EventKind.GameCanceled
    | EventKind.GameParametersChanged
    | EventKind.GameStarted
    | EventKind.PlayerJoined
    | EventKind.PlayerRemoved
    | EventKind.PlayerStatusChanged
    | EventKind.TurnCommitted
        -> true
    | _ -> false

let sendIfPublishable (notificationServ : INotificationService) (response : StateAndEventResponse) : Task<unit> =
    task {
        if isPublishable response.event
        then return! notificationServ.send response
        else return ()
    }

let processEvent
    (eventRepo : IEventRepository)
    (eventServ : EventService)
    (gameRepo : IGameRepository)
    (notificationServ : INotificationService)
    (gameId : int)
    (getCreateEventRequest : Game -> CreateEventRequest) : Task<StateAndEventResponse> =
    task {
        let! game = gameRepo.getGame gameId
        let eventRequest = getCreateEventRequest game
        let newGame = eventServ.applyEvent game eventRequest
        let! response = eventRepo.persistEvent (eventRequest, game, newGame)
        let! _ = sendIfPublishable notificationServ response
        return response
    }
