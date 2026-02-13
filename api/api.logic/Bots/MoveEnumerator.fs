module Djambi.Api.Logic.Bots.MoveEnumerator

open Djambi.Api.Model
open Djambi.Api.Enums

/// Get all legal complete moves for the current player.
/// Each move is a sequence of cell IDs that ends at AwaitingCommit.
let enumerateMoves (game : Game) : int list list =
    let rec explore (g : Game) (path : int list) : int list list =
        match g.currentTurn with
        | None -> []
        | Some turn ->
            match turn.status with
            | TurnStatus.AwaitingCommit ->
                // This path is a complete move
                [List.rev path]
            | TurnStatus.DeadEnd ->
                // Dead end, discard
                []
            | TurnStatus.AwaitingSelection ->
                turn.selectionOptions
                |> List.collect (fun cellId ->
                    try
                        let g' = GameSimulator.applySelection g cellId
                        explore g' (cellId :: path)
                    with
                    | _ -> [] // Skip invalid selections
                )
            | _ -> []

    explore game []
