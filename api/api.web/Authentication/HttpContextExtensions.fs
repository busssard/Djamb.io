namespace Djambi.Api.Web.Authentication

open System.Security.Authentication
open Microsoft.AspNetCore.Http
open Djambi.Api.Model

[<AutoOpen>]
module HttpContextExtensions =

    let private sessionKey = "DjambiSession"

    type HttpContext with
        member this.GetSession() : Session =
            match this.Items.TryGetValue(sessionKey) with
            | true, (:? Session as s) -> s
            | _ -> raise (AuthenticationException("Not signed in."))

        member this.GetSessionOption() : Session option =
            match this.Items.TryGetValue(sessionKey) with
            | true, (:? Session as s) -> Some s
            | _ -> None

        member this.SetSession(session : Session) : unit =
            this.Items.[sessionKey] <- session :> obj
