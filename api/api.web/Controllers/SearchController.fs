namespace Djambi.Api.Web.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Web.Authentication
open Djambi.Api.Web.Model
open Djambi.Api.Web.Mappings

[<ApiController>]
[<Authorize>]
[<Route("api/search")>]
type SearchController(manager : ISearchManager) =
    inherit ControllerBase()

    [<HttpPost("games")>]
    [<ProducesResponseType(200, Type = typeof<SearchGameDto[]>)>]
    member __.SearchGames([<FromBody>] query : GamesQueryDto) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let session = ctx.GetSession()
            let query = query |> toGamesQuery
            let! games = manager.searchGames query session
            let dtos = games |> List.map toSearchGameDto
            return OkObjectResult(dtos) :> IActionResult
        }
