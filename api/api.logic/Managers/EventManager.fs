namespace Djambi.Api.Logic.Managers

open Djambi.Api.Db.Interfaces
open Djambi.Api.Logic
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Model
open Djambi.Api.Enums
open System.Threading.Tasks

type EventManager(eventRepo : IEventRepository,
                  gameRepo : IGameRepository) =

    interface IEventManager with
        member x.getEvents (gameId, query) session =
            task {
                let! game = gameRepo.getGame gameId
                Security.ensurePlayerOrHas Privilege.ViewGames session game
                return! eventRepo.getEvents (gameId, query)
            }
