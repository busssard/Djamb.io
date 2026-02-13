namespace Djambi.Api.Web.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Web.Authentication
open Djambi.Api.Web.Model

[<ApiController>]
[<Authorize>]
[<Route("api/bots")>]
type BotController(registry : IBotRegistry, gameManager : IGameManager) =
    inherit ControllerBase()

    [<AllowAnonymous>]
    [<HttpGet>]
    [<ProducesResponseType(200, Type = typeof<BotInfoDto list>)>]
    member __.ListBots() : IActionResult =
        let bots =
            registry.listBots()
            |> List.map (fun b -> { name = b.name; description = b.description } : BotInfoDto)
        OkObjectResult(bots) :> IActionResult

    [<AllowAnonymous>]
    [<HttpGet("{name}")>]
    [<ProducesResponseType(200, Type = typeof<BotInfoDto>)>]
    [<ProducesResponseType(404)>]
    member __.GetBot(name : string) : IActionResult =
        match registry.getBot name with
        | Some bot ->
            let dto = { name = bot.info.name; description = bot.info.description } : BotInfoDto
            OkObjectResult(dto) :> IActionResult
        | None ->
            NotFoundResult() :> IActionResult

    [<HttpPost("{name}/select-cell")>]
    [<ProducesResponseType(200, Type = typeof<BotSelectCellResponseDto>)>]
    [<ProducesResponseType(404)>]
    member __.SelectCell(name : string, [<FromBody>] request : BotSelectCellRequestDto) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            match registry.getBot name with
            | None ->
                return NotFoundResult() :> IActionResult
            | Some bot ->
                let session = ctx.GetSession()
                let! game = gameManager.getGame request.gameId session
                let! cellId = bot.selectCell game request.playerId
                let dto = { cellId = cellId } : BotSelectCellResponseDto
                return OkObjectResult(dto) :> IActionResult
        }
