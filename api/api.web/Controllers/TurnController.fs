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
[<Route("api/games/{gameId}/current-turn")>]
type TurnController(manager : ITurnManager) =
    inherit ControllerBase()

    [<HttpPost("selection-request/{cellId}")>]
    [<ProducesResponseType(200, Type = typeof<StateAndEventResponseDto>)>]
    member __.SelectCell(gameId : int, cellId : int) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let session = ctx.GetSession()
            let! response = manager.selectCell (gameId, cellId) session
            let dto = response |> toStateAndEventResponseDto
            return OkObjectResult(dto) :> IActionResult
        }

    [<HttpPost("reset-request")>]
    [<ProducesResponseType(200, Type = typeof<StateAndEventResponseDto>)>]
    member __.ResetTurn(gameId : int) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let session = ctx.GetSession()
            let! response = manager.resetTurn gameId session
            let dto = response |> toStateAndEventResponseDto
            return OkObjectResult(dto) :> IActionResult
        }

    [<HttpPost("commit-request")>]
    [<ProducesResponseType(200, Type = typeof<StateAndEventResponseDto>)>]
    member __.CommitTurn(gameId : int) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let session = ctx.GetSession()
            let! response = manager.commitTurn gameId session
            let dto = response |> toStateAndEventResponseDto
            return OkObjectResult(dto) :> IActionResult
        }
