namespace Djambi.Api.Logic.Bots

open System.Collections.Generic
open Djambi.Api.Logic.Interfaces

type BotRegistry() =
    let bots = Dictionary<string, IBotPlayer>()

    interface IBotRegistry with
        member __.register (bot : IBotPlayer) =
            bots.[bot.info.name] <- bot

        member __.getBot (name : string) =
            match bots.TryGetValue(name) with
            | true, bot -> Some bot
            | false, _ -> None

        member __.listBots () =
            bots.Values
            |> Seq.map (fun b -> b.info)
            |> Seq.toList
