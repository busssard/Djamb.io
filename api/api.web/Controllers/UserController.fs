namespace Djambi.Api.Web.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Web
open Djambi.Api.Web.Authentication
open Djambi.Api.Web.Mappings
open Djambi.Api.Web.Model

[<ApiController>]
[<Authorize>]
[<Route("api/users")>]
type UserController(manager : IUserManager,
                    cookieProvider : CookieProvider) =
    inherit ControllerBase()

    [<AllowAnonymous>]
    [<HttpPost>]
    [<ProducesResponseType(200, Type = typeof<UserDto>)>]
    member __.CreateUser([<FromBody>] request : CreateUserRequestDto) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let sessionOption = ctx.GetSessionOption()
            let request = request |> toCreateUserRequest
            let! user = manager.createUser request sessionOption
            let dto = user |> toUserDto
            return OkObjectResult(dto) :> IActionResult
        }

    [<AllowAnonymous>]
    [<HttpPost("quick")>]
    [<ProducesResponseType(200, Type = typeof<SessionDto>)>]
    member __.QuickRegister([<FromBody>] request : QuickRegisterRequestDto) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let (name, email) = request |> toQuickRegisterArgs
            let! session = manager.quickRegister name email
            let dto = session |> toSessionDto
            cookieProvider.AppendCookie ctx (session.token, session.expiresOn)
            return OkObjectResult(dto) :> IActionResult
        }

    [<HttpDelete("{userId}")>]
    [<ProducesResponseType(200)>]
    member __.DeleteUser(userId : int) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let session = ctx.GetSession()
            let! response = manager.deleteUser userId session
            return OkObjectResult(response) :> IActionResult
        }

    [<HttpGet("{userId}")>]
    [<ProducesResponseType(200, Type = typeof<UserDto>)>]
    member __.GetUser(userId : int) : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let session = ctx.GetSession()
            let! user = manager.getUser userId session
            let dto = user |> toUserDto
            return OkObjectResult(dto) :> IActionResult
        }

    [<HttpGet("current")>]
    [<ProducesResponseType(200, Type = typeof<UserDto>)>]
    member __.GetCurrentUser() : Task<IActionResult> =
        let ctx = base.HttpContext
        task {
            let session = ctx.GetSession()
            let! user = manager.getCurrentUser session
            let dto = user |> toUserDto
            return OkObjectResult(dto) :> IActionResult
        }
