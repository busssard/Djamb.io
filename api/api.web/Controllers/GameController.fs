namespace Djambi.Api.Web.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Web.Authentication
open Djambi.Api.Web.Mappings
open Djambi.Api.Web.Model

[<ApiController>]
[<Authorize>]
[<Route("api/games")>]
type GameController(manager : IGameManager) =
    inherit ControllerBase()

    [<HttpGet("{gameId}")>]
    [<ProducesResponseType(200, Type = typeof<GameDto>)>]
    member __.GetGame(gameId : int) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let session = ctx.GetSession()
            let! game = manager.getGame gameId session
            let dto = game |> toGameDto
            return OkObjectResult(dto) :> IActionResult
        }

    [<HttpPost>]
    [<ProducesResponseType(200, Type = typeof<GameDto>)>]
    member __.CreateGame([<FromBody>] request : GameParametersDto) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let session = ctx.GetSession()
            let request = request |> toGameParameters
            let! game = manager.createGame request session
            let dto = game |> toGameDto
            return OkObjectResult(dto) :> IActionResult
        }

    [<HttpPut("{gameId}/parameters")>]
    [<ProducesResponseType(200, Type = typeof<StateAndEventResponseDto>)>]
    member __.UpdateGameParameters(gameId : int, [<FromBody>] parameters : GameParametersDto) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let session = ctx.GetSession()
            let parameters = parameters |> toGameParameters
            let! response = manager.updateGameParameters gameId parameters session
            let dto = response |> toStateAndEventResponseDto
            return OkObjectResult(dto) :> IActionResult
        }

    [<HttpPost("{gameId}/start-request")>]
    [<ProducesResponseType(200, Type = typeof<StateAndEventResponseDto>)>]
    member __.StartGame(gameId : int) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let session = ctx.GetSession()
            let! response = manager.startGame gameId session
            let dto = response |> toStateAndEventResponseDto
            return OkObjectResult(dto) :> IActionResult
        }
