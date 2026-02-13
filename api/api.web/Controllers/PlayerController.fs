namespace Djambi.Api.Web.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Web.Authentication
open Djambi.Api.Web.Mappings
open Djambi.Api.Web.Model
open Djambi.Api.Enums

[<ApiController>]
[<Authorize>]
[<Route("api/games/{gameId}/players")>]
type PlayerController(manager : IPlayerManager) =
    inherit ControllerBase()

    [<HttpPost>]
    [<ProducesResponseType(200, Type = typeof<StateAndEventResponseDto>)>]
    member __.AddPlayer(gameId : int, [<FromBody>] request : CreatePlayerRequestDto) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let session = ctx.GetSession()
            let request = request |> toCreatePlayerRequest
            let! response = manager.addPlayer gameId request session
            let dto = response |> toStateAndEventResponseDto
            return OkObjectResult(dto) :> IActionResult
        }

    [<HttpDelete("{playerId}")>]
    [<ProducesResponseType(200, Type = typeof<StateAndEventResponseDto>)>]
    member __.RemovePlayer(gameId : int, playerId : int) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let session = ctx.GetSession()
            let! response = manager.removePlayer (gameId, playerId) session
            let dto = response |> toStateAndEventResponseDto
            return OkObjectResult(dto) :> IActionResult
        }

    [<HttpPut("{playerId}/status/{status}")>]
    [<ProducesResponseType(200, Type = typeof<StateAndEventResponseDto>)>]
    member __.SetPlayerStatus(gameId : int, playerId : int, status : PlayerStatus) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let session = ctx.GetSession()
            let! response = manager.updatePlayerStatus (gameId, playerId, status) session
            let dto = response |> toStateAndEventResponseDto
            return OkObjectResult(dto) :> IActionResult
        }
