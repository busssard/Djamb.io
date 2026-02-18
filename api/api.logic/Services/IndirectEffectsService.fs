namespace Djambi.Api.Logic.Services

open System.Collections.Generic
open System.Linq
open Djambi.Api.Common.Collections
open Djambi.Api.Logic.ModelExtensions
open Djambi.Api.Logic.ModelExtensions.BoardModelExtensions
open Djambi.Api.Logic.ModelExtensions.GameModelExtensions
open Djambi.Api.Logic
open Djambi.Api.Logic.Services
open Djambi.Api.Model
open Djambi.Api.Enums

type private SuffocationCheckResult =
    | NotSuffocated
    | Suffocated
    | ForcedPass

type IndirectEffectsService(eventServ : EventService,
                            selectionOptionsServ : SelectionOptionsService) =

    let removeSequentialDuplicates(turnCycle : int list) : int list =
        if turnCycle.Length = 1 then turnCycle
        else
            let list = turnCycle.ToList()
            for i in [(list.Count-1)..1] do
                if list.[i] = list.[i-1]
                then list.RemoveAt(i)
            list |> Seq.toList

    let getAbandonPiecesEffects (game : Game, oldPlayerId : int) : Effect list =
        game.pieces
        |> List.filter (fun piece -> piece.playerId = Some oldPlayerId)
        |> List.map (fun p -> Effect.PieceAbandoned { oldPiece = p })

    let getEnlistPiecesEffects (game : Game, oldPlayerId : int option, newPlayerId : int) : Effect list =
        game.pieces
        |> List.filter (fun piece -> piece.playerId = oldPlayerId)
        |> List.map (fun p -> Effect.PieceEnlisted { oldPiece = p; newPlayerId = newPlayerId })

    let getEliminatePlayerEffects (game : Game) (playerId : int) (killingPlayerId : int option) : Effect list =
        let p = game.players |> List.find (fun p -> p.id = playerId)

        let effects = new ArrayList<Effect>()

        effects.Add(Effect.PlayerStatusChanged {
            playerId = p.id
            oldStatus = p.status
            newStatus = PlayerStatus.Eliminated
        })

        let newCycle =
            game.turnCycle
            |> List.filter (fun pId -> pId <> p.id)
            |> removeSequentialDuplicates

        effects.Add(Effect.TurnCyclePlayerRemoved {
            playerId = p.id
            oldValue = game.turnCycle
            newValue = newCycle
        })

        match killingPlayerId with
        | Some kpId ->
            effects.AddRange (getEnlistPiecesEffects (game, Some p.id, kpId))
        | None ->
            effects.AddRange (getAbandonPiecesEffects (game, p.id))

        effects |> Seq.toList

    let getClaimAbandonedPiecesEffects (game : Game) (newPlayerId : int) : Effect list =
        game.pieces
        |> List.filter (fun p -> p.playerId.IsNone && p.kind <> PieceKind.Corpse)
        |> List.map (fun p -> Effect.PieceEnlisted { oldPiece = p; newPlayerId = newPlayerId })

    let getConnectedGroup (game : Game) (board : BoardMetadata) (conduit : Piece) : Piece list =
        let pieceIndex = game.piecesIndexedByCell
        let visited = HashSet<int>()
        let queue = Queue<int>()
        let group = ArrayList<Piece>()

        queue.Enqueue(conduit.cellId)
        visited.Add(conduit.cellId) |> ignore
        group.Add(conduit)

        while queue.Count > 0 do
            let cellId = queue.Dequeue()
            let neighbors = board.neighborsFromCellId cellId
            for neighbor in neighbors do
                if not (visited.Contains neighbor.id) then
                    visited.Add(neighbor.id) |> ignore
                    match pieceIndex.TryFind neighbor.id with
                    | Some piece when piece.playerId = conduit.playerId && piece.kind <> PieceKind.Corpse ->
                        group.Add(piece)
                        queue.Enqueue(neighbor.id)
                    | _ -> ()

        group |> Seq.toList

    let checkSuffocation (game : Game) (playerId : int) : SuffocationCheckResult =
        let board = BoardModelUtility.getBoardMetadata game.parameters.regionCount

        //Find the player's original Conduit
        let conduit =
            game.pieces
            |> List.tryFind (fun p ->
                p.kind = PieceKind.Conduit
                && p.playerId = Some playerId
                && p.originalPlayerId = playerId)

        match conduit with
        | None -> NotSuffocated //No original conduit (shouldn't happen for alive players)
        | Some conduit ->
            //Chief on Field of Power is immune
            let conduitCell = board.cell conduit.cellId
            match conduitCell with
            | Some c when c.isCenter -> NotSuffocated
            | _ ->
                //BFS to find connected group
                let group = getConnectedGroup game board conduit

                //If Reaper is in the group, no suffocation
                if group |> List.exists (fun p -> p.kind = PieceKind.Reaper) then
                    NotSuffocated
                else
                    //Check if any piece in the group has a legal move
                    let hasLegalMove =
                        group |> List.exists (fun p ->
                            let strategy = Pieces.getStrategy game.parameters.rulesetKind p
                            strategy.isAlive && (selectionOptionsServ.getMoveOptionsForPiece(game, p)).Length > 0)

                    if hasLegalMove then
                        NotSuffocated
                    else
                        //No piece can move -- check what's blocking
                        let groupCellIds = group |> List.map (fun p -> p.cellId) |> Set.ofList
                        let pieceIndex = game.piecesIndexedByCell

                        let boundaryCells =
                            group
                            |> List.collect (fun p -> board.neighborsFromCellId p.cellId |> List.map (fun c -> c.id))
                            |> List.distinct
                            |> List.filter (fun cId -> not (groupCellIds.Contains cId))

                        let hasEnemyBlocker =
                            boundaryCells
                            |> List.exists (fun cId ->
                                match pieceIndex.TryFind cId with
                                | Some piece when piece.playerId.IsSome
                                                   && piece.playerId <> Some playerId
                                                   && piece.kind <> PieceKind.Corpse -> true
                                | _ -> false)

                        if hasEnemyBlocker then ForcedPass
                        else Suffocated

    let getSuffocationEffectsAfterCommit (game : Game) : Effect list =
        let effects = ArrayList<Effect>()
        let mutable game = game

        let alivePlayers = game.players |> List.filter (fun p -> p.status = PlayerStatus.Alive)
        for player in alivePlayers do
            match checkSuffocation game player.id with
            | Suffocated ->
                let fx = Effect.PlayerSuffocated { playerId = player.id } ::
                         (getEliminatePlayerEffects game player.id None)
                effects.AddRange fx
                game <- eventServ.applyEffects fx game
            | _ -> () //ForcedPass and NotSuffocated: handled at beginning-of-turn

        //Auto-claim: if a player's original Conduit is currently on the center, claim abandoned pieces
        let board = BoardModelUtility.getBoardMetadata game.parameters.regionCount
        let centerCell = board.cells() |> Seq.find (fun c -> c.isCenter)
        let conduitInCenter =
            game.pieces
            |> List.tryFind (fun p ->
                p.kind = PieceKind.Conduit
                && p.playerId.IsSome
                && p.playerId = Some p.originalPlayerId
                && p.cellId = centerCell.id)

        match conduitInCenter with
        | Some c ->
            let claimFx = getClaimAbandonedPiecesEffects game c.playerId.Value
            if not claimFx.IsEmpty then
                effects.AddRange claimFx
                game <- eventServ.applyEffects claimFx game
        | None -> ()

        effects |> Seq.toList

    let getRiseOrFallFromPowerEffects (game : Game, updatedGame : Game) : Effect list =
        //Keep only one occurrence of the given player, removing bonus turns
        let removeBonusTurnsForPlayer playerId (turns : int list) =
            let mutable kept = false
            turns
            |> List.filter (fun t ->
                if t <> playerId then true
                elif not kept then kept <- true; true
                else false)

        //Insert a turn for the given player before every turn, except the current one
        let addBonusTurnsForPlayer playerId turns =
            let stack = new Stack<int>()
            for t in turns |> Seq.skip(1) |> Seq.rev do
                stack.Push t
                stack.Push playerId
            let result = stack |> Seq.toList |> removeSequentialDuplicates
            // For 2-player games, the above produces [p1, p2] unchanged because
            // removeSequentialDuplicates collapses the adjacent duplicate.
            // We need [p1, p2, p1] so the powered player gets double turns.
            let distinctCount = result |> Seq.distinct |> Seq.length
            if distinctCount = 2 && result |> Seq.filter (fun t -> t = playerId) |> Seq.length < 2 then
                result @ [playerId]
            else
                result

        //A player in power has multiple turns
        let hasPower (g : Game) (pId : int) =
            let turns =
                g.turnCycle
                |> Seq.filter (fun n -> n = pId)
                |> Seq.length
            turns > 1

        //Players can rise to power if there are at least 2 left
        let powerCanBeHad (g : Game) =
            let players =
                g.turnCycle
                |> Seq.distinct
                |> Seq.length
            players >= 2

        let turn = updatedGame.currentTurn.Value
        let subject = (turn.subjectPiece game).Value
        let destination = (turn.destinationCell game.parameters.regionCount).Value
        let origin = (turn.subjectCell game.parameters.regionCount).Value
        let subjectStrategy = Pieces.getStrategy game.parameters.rulesetKind subject

        let effects = new ArrayList<Effect>()
        let mutable turns = updatedGame.turnCycle

        let subjectPlayerId = subject.playerId.Value
        if subjectStrategy.canStayInCenter
        then
            if origin.isCenter
                && not destination.isCenter
                && hasPower updatedGame subjectPlayerId
            then
                //If conduit subject leaves power, remove bonus turns
                let newTurns = removeBonusTurnsForPlayer subjectPlayerId turns
                effects.Add(Effect.TurnCyclePlayerFellFromPower { playerId = subjectPlayerId; oldValue = turns; newValue = newTurns })
                turns <- newTurns
            elif not origin.isCenter
                && destination.isCenter
                && powerCanBeHad updatedGame
            then
                //If conduit subject rises to power, add bonus turns
                let newTurns = addBonusTurnsForPlayer subjectPlayerId turns
                effects.Add(Effect.TurnCyclePlayerRoseToPower { playerId = subjectPlayerId; oldValue = turns; newValue = newTurns })
                turns <- newTurns
                //Claim any abandoned (gray) pieces
                effects.AddRange (getClaimAbandonedPiecesEffects updatedGame subjectPlayerId)
            else ()

        match (turn.targetPiece game, turn.dropCell game.parameters.regionCount) with
        | (Some target, Some drop) ->
            let targetStrategy = Pieces.getStrategy game.parameters.rulesetKind target

            if subjectStrategy.canEnterCenterToEvictPiece
                && not subjectStrategy.killsTarget
                && subjectStrategy.canDropTarget
                && targetStrategy.canStayInCenter
            then
                //If conduit target is moved out of power and not killed, remove bonus turns
                if destination.isCenter
                    && not drop.isCenter
                    && hasPower updatedGame subjectPlayerId
                then
                    let newTurns = removeBonusTurnsForPlayer subjectPlayerId turns
                    effects.Add(Effect.TurnCyclePlayerFellFromPower { playerId = subjectPlayerId; oldValue = turns; newValue = newTurns })
                    turns <- newTurns
                //If conduit target is dropped in power and not killed, add bonus turns
                elif not destination.isCenter
                    && drop.isCenter
                    && powerCanBeHad updatedGame
                then
                    let newTurns = addBonusTurnsForPlayer subjectPlayerId turns
                    effects.Add(Effect.TurnCyclePlayerRoseToPower { playerId = subjectPlayerId; oldValue = turns; newValue = newTurns })
                    turns <- newTurns
                else ()
            else ()
        | _ -> ()

        effects |> Seq.toList

    let getVictoryEffects (game : Game) : Effect list =
        let remainingPlayers = game.players |> List.filter (fun p -> p.status = PlayerStatus.Alive && p.userId.IsSome)
        if remainingPlayers.Length = 1
        then
            let p = remainingPlayers.[0]
            let finishConcedeEffects =
                game.players
                |> List.filter (fun p -> p.status = PlayerStatus.WillConcede)
                |> List.map (fun p -> Effect.PlayerStatusChanged {
                    playerId = p.id
                    oldStatus = PlayerStatus.WillConcede
                    newStatus = PlayerStatus.Conceded
                })
            let mainEffects = [
                Effect.PlayerStatusChanged { oldStatus = p.status; newStatus = PlayerStatus.Victorious; playerId = p.id}
                Effect.GameStatusChanged { oldValue = game.status; newValue = GameStatus.Over }
                Effect.CurrentTurnChanged { oldValue = game.currentTurn; newValue = None }
            ]
            List.append finishConcedeEffects mainEffects
        else []

    let getSecondaryEffectsForConcede (game : Game, request : PlayerStatusChangeRequest) : Effect list =
        let f = Effect.TurnCyclePlayerRemoved {
            playerId = request.playerId
            oldValue = game.turnCycle
            newValue = game.turnCycle |> List.filter (fun pId -> pId <> request.playerId)
        }
        f :: getAbandonPiecesEffects (game, request.playerId)

    let getBeginningOfNextTurnEffects (game : Game) : Effect list =
        let effects = new ArrayList<Effect>()

        let mutable game = game
        let mutable stop = false
        let mutable cycleCount = 0
        let maxCycles = game.turnCycle |> Seq.distinct |> Seq.length

        while game.turnCycle.Length > 1 && not stop && cycleCount <= maxCycles do
            let player = game.players |> List.find (fun p -> p.id = game.turnCycle.[0])
            //Check for players who conceded before their turn
            if player.status = PlayerStatus.WillConcede then
                let fx = Effect.PlayerStatusChanged { playerId = player.id; oldStatus = PlayerStatus.WillConcede; newStatus = PlayerStatus.Conceded } ::
                         getSecondaryEffectsForConcede(game, {playerId = player.id; gameId = game.id; status = PlayerStatus.Conceded})
                effects.AddRange fx
                game <- eventServ.applyEffects fx game
            else
            //Check for suffocation
                match checkSuffocation game player.id with
                | Suffocated ->
                    let fx = Effect.PlayerSuffocated { playerId = player.id } ::
                             (getEliminatePlayerEffects game player.id None)
                    effects.AddRange fx
                    game <- eventServ.applyEffects fx game
                | ForcedPass ->
                    //Skip this player's turn
                    let fx = [Effect.PlayerForcedPass { playerId = player.id }]
                    effects.AddRange fx
                    game <- eventServ.applyEffects fx game
                    let newCycle = game.turnCycle |> List.rotate 1
                    let advanceFx = Effect.TurnCycleAdvanced { oldValue = game.turnCycle; newValue = newCycle }
                    effects.Add advanceFx
                    game <- eventServ.applyEffect advanceFx game
                    cycleCount <- cycleCount + 1
                | NotSuffocated ->
            //Then check for out of moves
                    let selectionOptions = (selectionOptionsServ.getSelectableCellsFromState game)
                    if selectionOptions.IsEmpty then
                        let fx = Effect.PlayerOutOfMoves { playerId = player.id } ::
                                 (getEliminatePlayerEffects game player.id None)
                        effects.AddRange fx
                        game <- eventServ.applyEffects fx game
                    else
            //Stop when you find a player who isn't affected
                        stop <- true

        effects |> Seq.toList

    let getTernaryEffects (game : Game, advanceTurn : bool) : Effect list =
        let effects = new ArrayList<Effect>()
        let mutable game = { game with currentTurn = Some Turn.empty } //This is required so that selection options come back

        let victoryEffects = getVictoryEffects game
        effects.AddRange victoryEffects

        if victoryEffects.IsEmpty
        then
            if advanceTurn
            then
                let f = Effect.TurnCycleAdvanced { oldValue = game.turnCycle; newValue = game.turnCycle |> List.rotate 1 }
                effects.Add f
                game <- eventServ.applyEffect f game
            else ()

            let nextTurnEffects = getBeginningOfNextTurnEffects game
            effects.AddRange nextTurnEffects
            game <- eventServ.applyEffects nextTurnEffects game

            //Check for victory caused by players being out of moves
            let victoryEffects = getVictoryEffects game
            effects.AddRange victoryEffects
            game <- eventServ.applyEffects victoryEffects game

            if victoryEffects.IsEmpty
            then
                let seletionOptions = selectionOptionsServ.getSelectableCellsFromState game
                let turn = { Turn.empty with selectionOptions = seletionOptions; turnStartedAt = Some System.DateTime.UtcNow }
                effects.Add(Effect.CurrentTurnChanged { oldValue = game.currentTurn; newValue = Some turn })
            else ()
        else ()
        effects |> Seq.toList

    member x.getSkipTurnEffects (game : Game) : Effect list =
        getTernaryEffects (game, true)

    member x.getIndirectEffectsForTurnCommit (game : Game, updatedGame : Game) : Effect list =
        (*
            The order of effects is important, both for the implementation and clarity of the event log to users.

            [Eliminate target's player] (option)
                Change player status
                Remove player from turn cycle
                Enlist pieces controlled by player
            Enlist pieces if killing neutral Conduit (option)
            Player rises/falls from power (option)
            [Suffocation check] (option, after corpse placement)
                Player suffocated
                Change player status
                Remove from turn cycle
                Abandon pieces
                Auto-claim by player in power (option)

            Victory (option)
            Advance turn cycle
            [Suffocation/forced pass check] (option, at beginning of turn)
            [Eliminate player out of moves] (option, repeat as necessary)
                Change player status
                Remove from turn cycle
                Abandon pieces
            Victory due to out-of-moves (option)
            Current turn changed
        *)

        let effects = new ArrayList<Effect>()

        let turn = updatedGame.currentTurn.Value
        let subject = (turn.subjectPiece game).Value
        let subjectStrategy = Pieces.getStrategy game.parameters.rulesetKind subject

        let killConduitEffects =
            match turn.targetPiece game with
            | Some target ->
                let targetStrategy = Pieces.getStrategy game.parameters.rulesetKind target

                if subjectStrategy.killsTarget
                    && targetStrategy.killsControllingPlayerWhenKilled
                    && target.playerId = Some target.originalPlayerId //Only original chief triggers elimination, not liegelords
                then
                    // Use updatedGame (post-direct-effects) so the killed piece (now Corpse with playerId=None)
                    // is not included in enlist/abandon effects
                    if target.playerId.IsSome
                    then getEliminatePlayerEffects updatedGame target.playerId.Value subject.playerId
                    else getEnlistPiecesEffects (updatedGame, Some target.originalPlayerId, subject.playerId.Value)
                else []
            | _ -> []

        effects.AddRange killConduitEffects
        let updatedGame = eventServ.applyEffects killConduitEffects updatedGame

        let riseFallEffects = getRiseOrFallFromPowerEffects (game, updatedGame)
        effects.AddRange riseFallEffects
        let updatedGame = eventServ.applyEffects riseFallEffects updatedGame

        let suffocationEffects = getSuffocationEffectsAfterCommit updatedGame
        effects.AddRange suffocationEffects
        let updatedGame = eventServ.applyEffects suffocationEffects updatedGame

        effects.AddRange (getTernaryEffects (updatedGame, true))

        effects |> Seq.toList

    member x.getIndirectEffectsForConcede (game : Game, request : PlayerStatusChangeRequest) : Effect list =
        (*
            The order of effects is important, both for the implementation and clarity of the event log to users.

            Remove player from turn cycle
            Abandon pieces controlled by player
            Victory/draw (option)
            Reset current turn
            [Eliminate player out of moves] (option, repeat as necessary)
                Change player status
                Remove from turn cycle
                Abandon pieces
            Victory due to out-of-moves (option)
            Current turn changed
        *)
        let effects = new ArrayList<Effect>()
        effects.AddRange (getSecondaryEffectsForConcede (game, request))
        let game = eventServ.applyEffects effects game
        effects.AddRange (getTernaryEffects (game, false))
        effects |> Seq.toList