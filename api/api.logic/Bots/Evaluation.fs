module Djambi.Api.Logic.Bots.Evaluation

open Djambi.Api.Logic
open Djambi.Api.Logic.ModelExtensions
open Djambi.Api.Logic.ModelExtensions.BoardModelExtensions
open Djambi.Api.Logic.ModelExtensions.GameModelExtensions
open Djambi.Api.Model
open Djambi.Api.Enums

let private pieceValue (kind : PieceKind) : float =
    match kind with
    | PieceKind.Conduit   -> 100.0
    | PieceKind.Hunter    -> 30.0
    | PieceKind.Scientist -> 25.0
    | PieceKind.Diplomat  -> 20.0
    | PieceKind.Reaper    -> 15.0
    | PieceKind.Thug      -> 10.0
    | PieceKind.Corpse    -> 0.0
    | _                   -> 0.0

let private evaluateAlivePlayer (game : Game) (playerId : int) : float =
    let board = BoardModelUtility.getBoardMetadata game.parameters.regionCount
    let pieceIndex = game.piecesIndexedByCell

    let myPieces = game.pieces |> List.filter (fun p -> p.playerId = Some playerId)
    let enemyPieces = game.pieces |> List.filter (fun p -> p.playerId.IsSome && p.playerId.Value <> playerId && p.kind <> PieceKind.Corpse)

    // Material value for own pieces
    let myMaterial = myPieces |> List.sumBy (fun p -> pieceValue p.kind)

    // Subtract enemy material (weighted by 0.5)
    let enemyMaterial =
        enemyPieces
        |> List.sumBy (fun p -> pieceValue p.kind)

    // Center bonus: +50 if own Conduit is in center
    let centerBonus =
        myPieces
        |> List.tryFind (fun p -> p.kind = PieceKind.Conduit)
        |> Option.map (fun conduit ->
            match board.cell conduit.cellId with
            | Some c when c.isCenter -> 50.0
            | _ -> 0.0
        )
        |> Option.defaultValue 0.0

    // Conduit danger: -40 if own Conduit is reachable by an enemy piece
    let conduitDanger =
        myPieces
        |> List.tryFind (fun p -> p.kind = PieceKind.Conduit)
        |> Option.map (fun conduit ->
            let threatened =
                enemyPieces
                |> List.exists (fun ep ->
                    let strategy = Pieces.getStrategy ep
                    if strategy.canTargetWithMove then
                        let paths = board.pathsFromCellId ep.cellId
                        paths |> List.exists (fun path ->
                            path
                            |> List.take (min strategy.moveMaxDistance path.Length)
                            |> List.exists (fun cell ->
                                let blocked =
                                    match pieceIndex.TryFind cell.id with
                                    | None -> false
                                    | Some _ -> true
                                cell.id = conduit.cellId && not blocked
                            )
                        )
                    elif strategy.canTargetAfterMove then
                        let paths = board.pathsFromCellId ep.cellId
                        paths |> List.exists (fun path ->
                            path
                            |> List.take (min strategy.moveMaxDistance path.Length)
                            |> List.exists (fun cell ->
                                match pieceIndex.TryFind cell.id with
                                | None ->
                                    let neighbors = board.neighborsFromCellId cell.id |> List.map (fun c -> c.id) |> Set.ofList
                                    Set.contains conduit.cellId neighbors
                                | Some _ -> false
                            )
                        )
                    else false
                )
            if threatened then -40.0 else 0.0
        )
        |> Option.defaultValue 0.0

    // Survival bonus: +5 per own living piece
    let survivalBonus = float myPieces.Length * 5.0

    // Threat bonus: +25 for each enemy Conduit we threaten
    let threatBonus =
        let enemyConduits =
            enemyPieces
            |> List.filter (fun p -> p.kind = PieceKind.Conduit)
        enemyConduits
        |> List.sumBy (fun ec ->
            let threatened =
                myPieces
                |> List.exists (fun mp ->
                    let strategy = Pieces.getStrategy mp
                    if strategy.canTargetWithMove then
                        let paths = board.pathsFromCellId mp.cellId
                        paths |> List.exists (fun path ->
                            path
                            |> List.take (min strategy.moveMaxDistance path.Length)
                            |> List.exists (fun cell ->
                                let blocked =
                                    match pieceIndex.TryFind cell.id with
                                    | None -> false
                                    | Some occ -> occ.id <> ec.id
                                cell.id = ec.cellId && not blocked
                            )
                        )
                    elif strategy.canTargetAfterMove then
                        let paths = board.pathsFromCellId mp.cellId
                        paths |> List.exists (fun path ->
                            path
                            |> List.take (min strategy.moveMaxDistance path.Length)
                            |> List.exists (fun cell ->
                                match pieceIndex.TryFind cell.id with
                                | None ->
                                    let neighbors = board.neighborsFromCellId cell.id |> List.map (fun c -> c.id) |> Set.ofList
                                    Set.contains ec.cellId neighbors
                                | Some _ -> false
                            )
                        )
                    else false
                )
            if threatened then 25.0 else 0.0
        )

    myMaterial - (0.5 * enemyMaterial) + centerBonus + conduitDanger + survivalBonus + threatBonus

/// Evaluate position for given player. Higher = better.
let evaluate (game : Game) (playerId : int) : float =
    let player = game.players |> List.tryFind (fun p -> p.id = playerId)
    match player with
    | None -> -10000.0
    | Some pl ->
        if pl.status = PlayerStatus.Eliminated || pl.status = PlayerStatus.Conceded then
            -10000.0
        elif pl.status = PlayerStatus.Victorious then
            10000.0
        else
            evaluateAlivePlayer game playerId
