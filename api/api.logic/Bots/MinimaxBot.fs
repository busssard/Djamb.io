namespace Djambi.Api.Logic.Bots

open System
open System.Diagnostics
open System.Threading.Tasks
open Djambi.Api.Model
open Djambi.Api.Enums
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Logic.ModelExtensions.GameModelExtensions

/// Paranoid minimax bot with alpha-beta pruning.
/// Treats all opponents as a single adversary (minimizing the root player's evaluation).
type MinimaxBot(maxDepth : int) =
    let mutable cachedMoveSequence : int list option = None
    let mutable cachedTurnSelectionsCount = -1
    let timeLimitMs = 5000L

    let orderMoves (game : Game) (moves : int list list) : int list list =
        // Prioritize captures: moves whose first Move selection lands on an occupied cell
        let pieceIndex = game.piecesIndexedByCell
        moves
        |> List.sortBy (fun move ->
            if move.Length >= 2 then
                match pieceIndex.TryFind(move.[1]) with
                | Some _ -> 0  // Capture move - prioritize
                | None -> 1    // Non-capture
            else 1
        )

    let rec minimax (game : Game) (rootPlayerId : int) (depth : int) (alpha : float) (beta : float) (stopwatch : Stopwatch) : float =
        if stopwatch.ElapsedMilliseconds > timeLimitMs then
            Evaluation.evaluate game rootPlayerId
        elif depth <= 0 then
            Evaluation.evaluate game rootPlayerId
        elif game.status = GameStatus.Over || game.currentTurn.IsNone then
            Evaluation.evaluate game rootPlayerId
        else
            let moves = MoveEnumerator.enumerateMoves game
            if moves.IsEmpty then
                Evaluation.evaluate game rootPlayerId
            else
                let currentPlayerId = game.currentPlayerId
                let isMaximizing = currentPlayerId = rootPlayerId
                let orderedMoves = orderMoves game moves

                if isMaximizing then
                    let mutable a = alpha
                    let mutable bestVal = -infinity
                    let mutable i = 0
                    while i < orderedMoves.Length && a < beta && stopwatch.ElapsedMilliseconds <= timeLimitMs do
                        let move = orderedMoves.[i]
                        try
                            let nextGame = GameSimulator.applyMove game move
                            let value = minimax nextGame rootPlayerId (depth - 1) a beta stopwatch
                            if value > bestVal then bestVal <- value
                            if value > a then a <- value
                        with
                        | _ -> () // Skip moves that cause errors
                        i <- i + 1
                    if bestVal = -infinity then Evaluation.evaluate game rootPlayerId
                    else bestVal
                else
                    let mutable b = beta
                    let mutable bestVal = infinity
                    let mutable i = 0
                    while i < orderedMoves.Length && alpha < b && stopwatch.ElapsedMilliseconds <= timeLimitMs do
                        let move = orderedMoves.[i]
                        try
                            let nextGame = GameSimulator.applyMove game move
                            let value = minimax nextGame rootPlayerId (depth - 1) alpha b stopwatch
                            if value < bestVal then bestVal <- value
                            if value < b then b <- value
                        with
                        | _ -> ()
                        i <- i + 1
                    if bestVal = infinity then Evaluation.evaluate game rootPlayerId
                    else bestVal

    let findBestMove (game : Game) (playerId : int) : int list =
        let moves = MoveEnumerator.enumerateMoves game
        if moves.IsEmpty then
            failwith "No moves available"
        elif moves.Length = 1 then
            moves.[0]
        else
            let stopwatch = Stopwatch.StartNew()
            let orderedMoves = orderMoves game moves
            let mutable bestMove = orderedMoves.[0]
            let mutable bestVal = -infinity
            let mutable alpha = -infinity

            for move in orderedMoves do
                if stopwatch.ElapsedMilliseconds <= timeLimitMs then
                    try
                        let nextGame = GameSimulator.applyMove game move
                        let value = minimax nextGame playerId (maxDepth - 1) alpha infinity stopwatch
                        if value > bestVal then
                            bestVal <- value
                            bestMove <- move
                        if value > alpha then
                            alpha <- value
                    with
                    | _ -> () // Skip invalid moves

            bestMove

    interface IBotPlayer with
        member _.info =
            {
                name = "minimax"
                description = "Paranoid minimax with alpha-beta pruning. Searches 2 plies deep with position evaluation."
            }

        member _.selectCell (game : Game) (playerId : int) : Task<int> =
            let turn = game.currentTurn.Value
            let currentSelCount = turn.selections.Length

            // On first selection of a turn (Subject), run full search and cache the move
            if currentSelCount = 0 || cachedMoveSequence.IsNone || cachedTurnSelectionsCount >= currentSelCount then
                let bestMove = findBestMove game playerId
                cachedMoveSequence <- Some bestMove
                cachedTurnSelectionsCount <- 0

            match cachedMoveSequence with
            | Some moves when cachedTurnSelectionsCount < moves.Length ->
                let cellId = moves.[cachedTurnSelectionsCount]
                cachedTurnSelectionsCount <- cachedTurnSelectionsCount + 1
                // Verify the cell is actually in options; if not, fall back to first option
                if turn.selectionOptions |> List.contains cellId then
                    Task.FromResult(cellId)
                else
                    // Cached move doesn't match current state, clear and pick from options
                    cachedMoveSequence <- None
                    cachedTurnSelectionsCount <- -1
                    Task.FromResult(turn.selectionOptions.[0])
            | _ ->
                // Fallback: pick first available option
                cachedMoveSequence <- None
                cachedTurnSelectionsCount <- -1
                Task.FromResult(turn.selectionOptions.[0])
