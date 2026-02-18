namespace Djambi.Api.Web.Mappings

open System.Collections.Generic
open Djambi.Api.Enums
open Djambi.Api.Model
open Djambi.Api.Web.Model

[<AutoOpen>]
module GameMapping =
    
    let toGameParametersDto (source : GameParameters) : GameParametersDto =
        {
            allowGuests = source.allowGuests
            isPublic = source.isPublic
            description = source.description |> Option.toObj
            regionCount = source.regionCount
            rulesetKind = source.rulesetKind
            turnTimeLimitSeconds = source.turnTimeLimitSeconds |> Option.toNullable
        }

    let toGameParameters (source : GameParametersDto) : GameParameters =
        {
            allowGuests = source.allowGuests
            description = source.description |> Option.ofObj
            isPublic = source.isPublic
            regionCount = source.regionCount
            rulesetKind = if int source.rulesetKind = 0 then RulesetKind.TotalWar else source.rulesetKind
            turnTimeLimitSeconds = source.turnTimeLimitSeconds |> Option.ofNullable |> Option.map int
        }

    let toPieceDto (source : Piece) : PieceDto =
        {
            id = source.id
            cellId = source.cellId
            kind = source.kind
            playerId = source.playerId |> Option.toNullable
            originalPlayerId = source.originalPlayerId
        }

    let toGameDto (source : Game) : GameDto =
        {
            id = source.id
            createdBy = source.createdBy |> toCreationSourceDto
            currentTurn = source.currentTurn |> Option.map toTurnDto |> Option.toObj
            parameters = source.parameters |> toGameParametersDto
            pieces = source.pieces |> List.map toPieceDto
            players = source.players |> List.map toPlayerDto
            status = source.status
            turnCycle = source.turnCycle
            inviteCode = source.inviteCode |> Option.toObj
            botAssignments =
                if source.botAssignments.IsEmpty then null
                else
                    let dict = Dictionary<int, string>()
                    source.botAssignments |> Map.iter (fun k v -> dict.Add(k, v))
                    dict
        }
        