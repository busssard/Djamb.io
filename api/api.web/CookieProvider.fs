namespace Djambi.Api.Web

open System
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Options
open Djambi.Api.Common
open Djambi.Api.Model.Configuration
open Microsoft.Extensions.Primitives

type CookieProvider(options : IOptions<ApiSettings>) =
    let cookieName = options.Value.cookieName

    member __.AppendCookie (ctx : HttpContext) (token : string, expiration : DateTime) =
        let cookieOptions = CookieOptions()
        cookieOptions.Domain <- options.Value.cookieDomain
        cookieOptions.Path <- "/"
        cookieOptions.Secure <- options.Value.secureCookies
        cookieOptions.HttpOnly <- true
        cookieOptions.SameSite <- SameSiteMode.Lax
        cookieOptions.Expires <- DateTimeOffset(expiration) |> Nullable.ofValue
        ctx.Response.Cookies.Append(cookieName, token, cookieOptions);
        ctx.Response.Headers.Add("Access-Control-Expose-Headers", StringValues("Set-Cookie"))

    member __.AppendEmptyCookie (ctx : HttpContext) =
        let cookieOptions = CookieOptions()
        cookieOptions.Domain <- options.Value.cookieDomain
        cookieOptions.Path <- "/"
        ctx.Response.Cookies.Delete(cookieName, cookieOptions)
