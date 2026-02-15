namespace Djambi.Api.Logic.Services

open System.Threading.Tasks
open Djambi.Api.Logic.Interfaces
open Serilog

type ConsoleEmailService() =
    interface IEmailService with
        member __.sendMagicLink email link =
            Log.Information("Magic link for {Email}: {Link}", email, link)
            Task.FromResult(())
