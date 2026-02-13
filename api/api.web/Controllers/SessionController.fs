namespace Djambi.Api.Web.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Web
open Djambi.Api.Web.Authentication
open Djambi.Api.Web.Model
open Djambi.Api.Web.Mappings

[<ApiController>]
[<AllowAnonymous>]
[<Route("api/sessions")>]
type SessionController(manager : ISessionManager,
                       cookieProvider : CookieProvider) =
    inherit ControllerBase()

    [<HttpPost>]
    [<ProducesResponseType(200, Type = typeof<SessionDto>)>]
    member __.OpenSession([<FromBody>] request : LoginRequestDto) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let sessionOption = ctx.GetSessionOption()
            match sessionOption with
            | Some s -> do! manager.logout s
            | None -> ()

            let request = request |> toLoginRequest
            let! session = manager.login request
            let dto = session |> toSessionDto

            cookieProvider.AppendCookie ctx (session.token, session.expiresOn)

            return OkObjectResult(dto) :> IActionResult
        }

    [<HttpDelete>]
    [<ProducesResponseType(204)>]
    member __.CloseSession() : Task<IActionResult> =
        let ctx = base.HttpContext

        // Always clear the cookie, even if the DB does not have a session matching it
        cookieProvider.AppendEmptyCookie ctx

        task {
            let sessionOption = ctx.GetSessionOption()
            match sessionOption with
            | Some s -> do! manager.logout s
            | None -> ()

            return NoContentResult() :> IActionResult
        }
