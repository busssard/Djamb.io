namespace Djambi.Api.Logic.Managers

open System
open Djambi.Api.Common.Control
open Djambi.Api.Db.Interfaces
open Djambi.Api.Logic
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Logic.Services
open Djambi.Api.Model
open Djambi.Api.Enums
open System.Threading.Tasks

type GameManager(eventRepo : IEventRepository,
                 eventServ : EventService,
                 gameCrudServ : GameCrudService,
                 gameRepo : IGameRepository,
                 gameStartServ : GameStartService,
                 notificationServ : INotificationService) =

    let isGameViewableByActiveUser (session : Session) (game : Game) : bool =
        let self = session.user
        game.parameters.isPublic
        || game.createdBy.userId = self.id
        || game.players |> List.exists(fun p -> p.userId = Some self.id)

    let processGameStartEventsAsync (gameId : int) (getCreateEventRequests : Game -> Task<(CreateEventRequest option * CreateEventRequest)>): Task<StateAndEventResponse> =
        task {
            let! game = gameRepo.getGame gameId
            let! eventRequests = getCreateEventRequests game
            let (addNeutralPlayers, startGame) = eventRequests;
            let! response =
                match addNeutralPlayers with
                | Some er ->
                    let newGame = eventServ.applyEvent game er
                    eventRepo.persistEvent (er, game, newGame)
                | None ->
                    let dummyEvent : Event = {
                        id = 0
                        createdBy = {
                            userId = 0
                            userName = ""
                            time = DateTime.MinValue
                        }
                        actingPlayerId = None
                        kind = EventKind.PlayerJoined
                        effects = []
                    }
                    Task.FromResult{ game = game; event = dummyEvent }

            let newGame = eventServ.applyEvent response.game startGame
            let! response = eventRepo.persistEvent (startGame, response.game, newGame)
            let! _ = EventProcessing.sendIfPublishable notificationServ response
            return response
        }

    interface IGameManager with
        member x.getGame gameId session =
            task {
                let! game = gameRepo.getGame gameId
                if isGameViewableByActiveUser session game
                then return game
                else return raise <| NotFoundException("Game not found.")
            }

        member x.getGameByInviteCode code =
            gameRepo.getGameByInviteCode code

        member x.createGame parameters session =
            gameCrudServ.createGame parameters session

        member x.updateGameParameters gameId parameters session =
            EventProcessing.processEvent eventRepo eventServ gameRepo notificationServ
                gameId (fun game ->
                    gameCrudServ.getUpdateGameParametersEvent (game, parameters) session
                )

        member x.startGame gameId session =
            processGameStartEventsAsync gameId (fun game -> gameStartServ.getGameStartEvents game session)
