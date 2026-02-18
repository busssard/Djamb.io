namespace Djambi.Api.Logic.Bots

open System.Diagnostics
open System.Threading.Tasks
open Djambi.Api.Model
open Djambi.Api.Enums
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Logic.ModelExtensions.GameModelExtensions

/// Max^N bot: each player maximizes their own score.
/// More realistic multiplayer reasoning than paranoid minimax.
type MaxNBot(maxDepth : int) =
    let mutable cachedMoveSequence : int list option = None
    let mutable cachedTurnSelectionsCount = -1
    let timeLimitMs = 5000L

    let orderMoves (game : Game) (moves : int list list) : int list list =
        let pieceIndex = game.piecesIndexedByCell
        moves
        |> List.sortBy (fun move ->
            if move.Length >= 2 then
                match pieceIndex.TryFind(move.[1]) with
                | Some _ -> 0  // Capture move - prioritize
                | None -> 1
            else 1
        )

    /// Evaluate position for ALL alive players, returning a Map<playerId, float>
    let evaluateAll (game : Game) : Map<int, float> =
        game.players
        |> List.filter (fun p ->
            p.status = PlayerStatus.Alive
            || p.status = PlayerStatus.Eliminated
            || p.status = PlayerStatus.Victorious)
        |> List.map (fun p -> p.id, Evaluation.evaluate game p.id)
        |> Map.ofList

    /// Max^N search: each player maximizes their own score.
    /// Returns score vector (Map<playerId, float>).
    let rec maxn (game : Game) (depth : int) (sw : Stopwatch) : Map<int, float> =
        if sw.ElapsedMilliseconds > timeLimitMs || depth <= 0
           || game.status = GameStatus.Over || game.currentTurn.IsNone then
            evaluateAll game
        else
            let moves = MoveEnumerator.enumerateMoves game
            if moves.IsEmpty then evaluateAll game
            else
                let currentPlayerId = game.currentPlayerId
                let orderedMoves = orderMoves game moves
                let mutable bestScores = Map.empty
                let mutable bestVal = -infinity
                for move in orderedMoves do
                    if sw.ElapsedMilliseconds <= timeLimitMs then
                        try
                            let nextGame = GameSimulator.applyMove game move
                            let scores = maxn nextGame (depth - 1) sw
                            let myScore =
                                scores
                                |> Map.tryFind currentPlayerId
                                |> Option.defaultValue -10000.0
                            if myScore > bestVal then
                                bestVal <- myScore
                                bestScores <- scores
                        with _ -> ()
                if bestScores.IsEmpty then evaluateAll game
                else bestScores

    let findBestMove (game : Game) (playerId : int) : int list =
        let moves = MoveEnumerator.enumerateMoves game
        if moves.IsEmpty then
            failwith "No moves available"
        elif moves.Length = 1 then
            moves.[0]
        else
            let sw = Stopwatch.StartNew()
            let orderedMoves = orderMoves game moves
            let mutable bestMove = orderedMoves.[0]
            let mutable bestVal = -infinity
            for move in orderedMoves do
                if sw.ElapsedMilliseconds <= timeLimitMs then
                    try
                        let nextGame = GameSimulator.applyMove game move
                        let scores = maxn nextGame (maxDepth - 1) sw
                        let myScore =
                            scores
                            |> Map.tryFind playerId
                            |> Option.defaultValue -10000.0
                        if myScore > bestVal then
                            bestVal <- myScore
                            bestMove <- move
                    with _ -> ()
            bestMove

    interface IBotPlayer with
        member _.info =
            {
                name = "maxn"
                description = "Max^N algorithm. Each player maximizes their own score for more realistic multiplayer reasoning."
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
                if turn.selectionOptions |> List.contains cellId then
                    Task.FromResult(cellId)
                else
                    cachedMoveSequence <- None
                    cachedTurnSelectionsCount <- -1
                    Task.FromResult(turn.selectionOptions.[0])
            | _ ->
                cachedMoveSequence <- None
                cachedTurnSelectionsCount <- -1
                Task.FromResult(turn.selectionOptions.[0])
