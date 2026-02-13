namespace Djambi.Api.Web.Authentication

open System
open System.Security.Claims
open System.Text.Encodings.Web
open System.Threading.Tasks
open Microsoft.AspNetCore.Authentication
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Model.Configuration
open Djambi.Api.Web

type DjambiAuthHandler(options : IOptionsMonitor<AuthenticationSchemeOptions>,
                       loggerFactory : ILoggerFactory,
                       encoder : UrlEncoder,
                       sessionService : ISessionService,
                       cookieProvider : CookieProvider,
                       apiSettings : IOptions<ApiSettings>) =
    inherit AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)

    override this.HandleAuthenticateAsync() : Task<AuthenticateResult> =
        // Capture protected members before entering task CE (F# closure limitation)
        let request = this.Request
        let context = this.Context
        let logger = this.Logger

        let cookieName = apiSettings.Value.cookieName
        let token = request.Cookies.[cookieName]

        if String.IsNullOrEmpty token then
            Task.FromResult(AuthenticateResult.NoResult())
        else
            task {
                try
                    match! sessionService.getAndRenewSession token with
                    | Some session ->
                        context.SetSession(session)

                        let claims = [
                            Claim(ClaimTypes.NameIdentifier, string session.user.id)
                            Claim(ClaimTypes.Name, session.user.name)
                        ]
                        let identity = ClaimsIdentity(claims, "DjambiSession")
                        let principal = ClaimsPrincipal(identity)
                        let ticket = AuthenticationTicket(principal, "DjambiSession")
                        return AuthenticateResult.Success(ticket)
                    | None ->
                        cookieProvider.AppendEmptyCookie context
                        return AuthenticateResult.NoResult()
                with
                | ex ->
                    logger.LogError(ex, "Error validating session")
                    cookieProvider.AppendEmptyCookie context
                    return AuthenticateResult.Fail(ex)
            }

[<AutoOpen>]
module AuthenticationExtensions =

    open Microsoft.Extensions.DependencyInjection

    type IServiceCollection with
        member this.AddDjambiAuthentication() : IServiceCollection =
            this
                .AddAuthentication("DjambiSession")
                .AddScheme<AuthenticationSchemeOptions, DjambiAuthHandler>("DjambiSession", fun _ -> ())
                |> ignore
            this.AddAuthorization() |> ignore
            this
