module Djambi.Api.Logic.Bots.GameSimulator

open System.Linq
open Djambi.Api.Common.Collections
open Djambi.Api.Logic
open Djambi.Api.Logic.ModelExtensions
open Djambi.Api.Logic.ModelExtensions.BoardModelExtensions
open Djambi.Api.Logic.ModelExtensions.GameModelExtensions
open Djambi.Api.Logic.Services
open Djambi.Api.Model
open Djambi.Api.Enums
open System.Collections.Generic

// Shared SelectionOptionsService instance (stateless, safe to reuse)
let private selectionOptionsServ = SelectionOptionsService()

// ────────────────────────────────────────────────────────────────
// Selection simulation (mirrors SelectionService logic)
// ────────────────────────────────────────────────────────────────

let private getSubjectSelectionDetails (game : Game) (cellId : int) =
    let pieces = game.piecesIndexedByCell
    match pieces.TryFind(cellId) with
    | None -> failwith "No piece in cell"
    | Some p ->
        let selection = Selection.subject(cellId, p.id)
        (selection, TurnStatus.AwaitingSelection, Some SelectionKind.Move)

let private getMoveSelectionDetails (game : Game) (cellId : int) =
    let pieces = game.piecesIndexedByCell
    let board = BoardModelUtility.getBoardMetadata game.parameters.regionCount
    let subject = game.currentTurn.Value.subjectPiece(game).Value
    let subjectStrategy = Pieces.getStrategy game.parameters.rulesetKind subject
    match pieces.TryFind(cellId) with
    | None ->
        let selection = Selection.move(cellId)
        if subjectStrategy.canTargetAfterMove
            && board.neighborsFromCellId cellId
                |> Seq.map (fun c -> pieces.TryFind c.id)
                |> Seq.values
                |> Seq.exists (fun p ->
                    let str = Pieces.getStrategy game.parameters.rulesetKind p
                    str.isAlive && p.playerId <> subject.playerId
                )
        then (selection, TurnStatus.AwaitingSelection, Some SelectionKind.Target)
        else (selection, TurnStatus.AwaitingCommit, None)
    | Some target ->
        let selection = Selection.moveWithTarget(cellId, target.id)
        if subjectStrategy.movesTargetToOrigin then
            match board.cell cellId with
            | None -> failwith "Cell not found"
            | Some c when c.isCenter ->
                (selection, TurnStatus.AwaitingSelection, Some SelectionKind.Vacate)
            | _ ->
                (selection, TurnStatus.AwaitingCommit, None)
        else (selection, TurnStatus.AwaitingSelection, Some SelectionKind.Drop)

let private getTargetSelectionDetails (game : Game) (cellId : int) =
    let pieces = game.piecesIndexedByCell
    match pieces.TryFind(cellId) with
    | None -> failwith "No piece in cell"
    | Some target ->
        let selection = Selection.target(cellId, target.id)
        (selection, TurnStatus.AwaitingCommit, None)

let private getDropSelectionDetails (game : Game) (cellId : int) =
    let turn = game.currentTurn.Value
    let subject = turn.subjectPiece(game).Value
    let destination = turn.destinationCell(game.parameters.regionCount).Value
    let subjectStrategy = Pieces.getStrategy game.parameters.rulesetKind subject
    let selection = Selection.drop(cellId)
    if subjectStrategy.canEnterCenterToEvictPiece
        && (not subjectStrategy.canStayInCenter)
        && destination.isCenter
    then (selection, TurnStatus.AwaitingSelection, Some SelectionKind.Vacate)
    else (selection, TurnStatus.AwaitingCommit, None)

let private getVacateSelectionDetails (_game : Game) (cellId : int) =
    let selection = Selection.vacate(cellId)
    (selection, TurnStatus.AwaitingCommit, None)

/// Apply a cell selection to the game, returning updated game state.
/// This mirrors SelectionService.getCellSelectedEvent but without DB/session.
let applySelection (game : Game) (cellId : int) : Game =
    let currentTurn = game.currentTurn.Value
    let (selection, turnStatus, requiredSelectionKind) =
        match currentTurn.requiredSelectionKind with
        | Some SelectionKind.Subject -> getSubjectSelectionDetails game cellId
        | Some SelectionKind.Move -> getMoveSelectionDetails game cellId
        | Some SelectionKind.Target -> getTargetSelectionDetails game cellId
        | Some SelectionKind.Drop -> getDropSelectionDetails game cellId
        | Some SelectionKind.Vacate -> getVacateSelectionDetails game cellId
        | _ -> failwith "Invalid selection kind"

    let turn =
        {
            status = turnStatus
            selections = List.append currentTurn.selections [selection]
            selectionOptions = []
            requiredSelectionKind = requiredSelectionKind
            turnStartedAt = currentTurn.turnStartedAt
        }
    let updatedGame = { game with currentTurn = Some turn }
    let options = selectionOptionsServ.getSelectableCellsFromState updatedGame
    let turn =
        if options.IsEmpty && turn.status = TurnStatus.AwaitingSelection
        then Turn.deadEnd turn.selections
        else { turn with selectionOptions = options }
    { updatedGame with currentTurn = Some turn }

// ────────────────────────────────────────────────────────────────
// Commit simulation (mirrors TurnService + IndirectEffectsService)
// ────────────────────────────────────────────────────────────────

let private removeSequentialDuplicates (turnCycle : int list) : int list =
    if turnCycle.Length <= 1 then turnCycle
    else
        let list = turnCycle.ToList()
        for i in [(list.Count-1) .. -1 .. 1] do
            if list.[i] = list.[i-1]
            then list.RemoveAt(i)
        list |> Seq.toList

let private getPrimaryEffects (game : Game) : (Game -> Game) =
    let currentTurn = game.currentTurn.Value
    match (currentTurn.subjectPiece game, currentTurn.destinationCell game.parameters.regionCount) with
    | (None, _) | (_, None) -> id
    | (Some subject, Some destination) ->
        let originCellId = subject.cellId
        let subjectStrategy = Pieces.getStrategy game.parameters.rulesetKind subject

        fun (g : Game) ->
            let mutable pieces = g.pieces |> List.map (fun p -> (p.id, p)) |> dict |> Dictionary

            // Move subject
            match currentTurn.vacateCellId with
            | None ->
                pieces.[subject.id] <- { pieces.[subject.id] with cellId = destination.id }
            | Some vacateCellId ->
                pieces.[subject.id] <- { pieces.[subject.id] with cellId = vacateCellId }

            // Handle target
            match currentTurn.targetPiece game with
            | None -> ()
            | Some target ->
                // Kill target if applicable
                if subjectStrategy.killsTarget then
                    pieces.[target.id] <- { pieces.[target.id] with kind = PieceKind.Corpse; playerId = None }

                // Drop target
                match currentTurn.dropCellId with
                | Some dropCellId ->
                    pieces.[target.id] <- { pieces.[target.id] with cellId = dropCellId }
                | None -> ()

                // Move target to origin (hunter)
                if subjectStrategy.movesTargetToOrigin then
                    pieces.[target.id] <- { pieces.[target.id] with cellId = originCellId }

            { g with pieces = pieces.Values |> Seq.toList }

let private getEliminatePlayerUpdate (playerId : int) (killingPlayerId : int option) (game : Game) : Game =
    let mutable g = game
    // Set player eliminated
    g <- { g with
            players = g.players |> List.replaceIf
                (fun p -> p.id = playerId)
                (fun p -> { p with status = PlayerStatus.Eliminated })
         }
    // Remove from turn cycle
    let newCycle =
        g.turnCycle
        |> List.filter (fun pId -> pId <> playerId)
        |> removeSequentialDuplicates
    g <- { g with turnCycle = newCycle }
    // Enlist or abandon pieces
    match killingPlayerId with
    | Some kpId ->
        g <- { g with
                pieces = g.pieces |> List.replaceIf
                    (fun p -> p.playerId = Some playerId)
                    (fun p -> { p with playerId = Some kpId })
             }
    | None ->
        g <- { g with
                pieces = g.pieces |> List.replaceIf
                    (fun p -> p.playerId = Some playerId)
                    (fun p -> { p with playerId = None })
             }
    g

let private applyRiseOrFallFromPower (game : Game) (updatedGame : Game) : Game =
    let removeBonusTurnsForPlayer playerId turns =
        let stack = Stack<int>()
        let mutable hasAddedTargetPlayer = false
        for t in turns |> Seq.rev do
            if t = playerId && not hasAddedTargetPlayer then
                hasAddedTargetPlayer <- true
                stack.Push t
            else stack.Push t
        stack |> Seq.toList |> removeSequentialDuplicates

    let addBonusTurnsForPlayer playerId turns =
        let stack = Stack<int>()
        for t in turns |> Seq.skip(1) |> Seq.rev do
            stack.Push t
            stack.Push playerId
        stack |> Seq.toList |> removeSequentialDuplicates

    let hasPower (g : Game) (pId : int) =
        g.turnCycle |> Seq.filter (fun n -> n = pId) |> Seq.length > 1

    let powerCanBeHad (g : Game) =
        g.turnCycle |> Seq.distinct |> Seq.length > 2

    let turn = updatedGame.currentTurn.Value
    let subject = (turn.subjectPiece game).Value
    let destination = (turn.destinationCell game.parameters.regionCount).Value
    let origin = (turn.subjectCell game.parameters.regionCount).Value
    let subjectStrategy = Pieces.getStrategy game.parameters.rulesetKind subject

    let mutable result = updatedGame
    let mutable turns = updatedGame.turnCycle
    let subjectPlayerId = subject.playerId.Value

    if subjectStrategy.canStayInCenter then
        if origin.isCenter && not destination.isCenter && hasPower updatedGame subjectPlayerId then
            turns <- removeBonusTurnsForPlayer subjectPlayerId turns
            result <- { result with turnCycle = turns }
        elif not origin.isCenter && destination.isCenter && powerCanBeHad updatedGame then
            turns <- addBonusTurnsForPlayer subjectPlayerId turns
            result <- { result with turnCycle = turns }

    match (turn.targetPiece game, turn.dropCell game.parameters.regionCount) with
    | (Some target, Some drop) ->
        let targetStrategy = Pieces.getStrategy game.parameters.rulesetKind target
        if subjectStrategy.canEnterCenterToEvictPiece
            && not subjectStrategy.killsTarget
            && subjectStrategy.canDropTarget
            && targetStrategy.canStayInCenter
        then
            if destination.isCenter && not drop.isCenter && hasPower updatedGame subjectPlayerId then
                turns <- removeBonusTurnsForPlayer subjectPlayerId turns
                result <- { result with turnCycle = turns }
            elif not destination.isCenter && drop.isCenter && powerCanBeHad updatedGame then
                turns <- addBonusTurnsForPlayer subjectPlayerId turns
                result <- { result with turnCycle = turns }
    | _ -> ()

    result

let private getVictoryCheck (game : Game) : Game =
    let remainingPlayers = game.players |> List.filter (fun p -> p.status = PlayerStatus.Alive && p.userId.IsSome)
    if remainingPlayers.Length = 1 then
        let p = remainingPlayers.[0]
        let g =
            { game with
                players = game.players |> List.replaceIf
                    (fun pl -> pl.status = PlayerStatus.WillConcede)
                    (fun pl -> { pl with status = PlayerStatus.Conceded })
            }
        let g =
            { g with
                players = g.players |> List.replaceIf
                    (fun pl -> pl.id = p.id)
                    (fun pl -> { pl with status = PlayerStatus.Victorious })
            }
        { g with status = GameStatus.Over; currentTurn = None }
    else game

let private advanceTurnAndCheckBeginning (game : Game) (advanceTurn : bool) : Game =
    let mutable g = { game with currentTurn = Some Turn.empty }
    let g' = getVictoryCheck g
    if g'.status = GameStatus.Over then g'
    else
        if advanceTurn then
            g <- { g with turnCycle = g.turnCycle |> List.rotate 1 }

        // Check beginning of next turn: concessions and out-of-moves
        let mutable stop = false
        while g.turnCycle.Length > 1 && not stop do
            let player = g.players |> List.find (fun p -> p.id = g.turnCycle.[0])
            if player.status = PlayerStatus.WillConcede then
                g <- { g with
                        players = g.players |> List.replaceIf
                            (fun p -> p.id = player.id)
                            (fun p -> { p with status = PlayerStatus.Conceded })
                     }
                // Remove from cycle and abandon pieces
                g <- { g with
                        turnCycle = g.turnCycle |> List.filter (fun pId -> pId <> player.id)
                        pieces = g.pieces |> List.replaceIf
                            (fun p -> p.playerId = Some player.id)
                            (fun p -> { p with playerId = None })
                     }
            else
                let options = selectionOptionsServ.getSelectableCellsFromState g
                if options.IsEmpty then
                    g <- getEliminatePlayerUpdate player.id None g
                else
                    stop <- true

        let g' = getVictoryCheck g
        if g'.status = GameStatus.Over then g'
        else
            let options = selectionOptionsServ.getSelectableCellsFromState g
            let turn = { Turn.empty with selectionOptions = options }
            { g with currentTurn = Some turn }

/// Commit the current turn, applying all effects and advancing to next player.
let applyCommit (game : Game) : Game =
    let applyPrimary = getPrimaryEffects game
    let mutable g = applyPrimary game

    // Handle conduit assassination -> eliminate player
    let turn = game.currentTurn.Value
    let subject = (turn.subjectPiece game).Value
    let subjectStrategy = Pieces.getStrategy game.parameters.rulesetKind subject

    match turn.targetPiece game with
    | Some target ->
        let targetStrategy = Pieces.getStrategy game.parameters.rulesetKind target
        if subjectStrategy.killsTarget && targetStrategy.killsControllingPlayerWhenKilled then
            if target.playerId.IsSome then
                g <- getEliminatePlayerUpdate target.playerId.Value subject.playerId g
            else
                // Enlist pieces of the original player
                g <- { g with
                        pieces = g.pieces |> List.replaceIf
                            (fun p -> p.playerId = Some target.originalPlayerId)
                            (fun p -> { p with playerId = Some subject.playerId.Value })
                     }
    | None -> ()

    // Rise/fall from power
    // For rise/fall we need the updated game with primary effects applied
    // but the turn still references the original selections
    let gWithTurn = { g with currentTurn = game.currentTurn }
    let gAfterPower = applyRiseOrFallFromPower game gWithTurn
    let g = { gAfterPower with currentTurn = game.currentTurn }

    // Advance turn and check next
    advanceTurnAndCheckBeginning g true

/// Apply a complete move (all selections + commit).
let applyMove (game : Game) (selections : int list) : Game =
    let mutable g = game
    for cellId in selections do
        g <- applySelection g cellId
    applyCommit g
