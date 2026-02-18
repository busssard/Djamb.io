namespace Djambi.Api.Logic.Services

open System
open System.Linq
open Djambi.Api.Common
open Djambi.Api.Common.Collections
open Djambi.Api.Common.Control
open Djambi.Api.Logic.ModelExtensions
open Djambi.Api.Logic.ModelExtensions.BoardModelExtensions
open Djambi.Api.Logic.Services
open Djambi.Api.Model
open Djambi.Api.Logic
open Djambi.Api.Enums
open System.Threading.Tasks

type GameStartService(playerServ : PlayerService,
                      selectionOptionsServ : SelectionOptionsService) =

    member x.getGameStartEvents (game : Game) (session : Session) : Task<(CreateEventRequest option * CreateEventRequest)> =
        Security.ensureCreatorOrEditPendingGames session game
        let participantCount =
            game.players
            |> List.filter (fun p ->
                p.kind <> PlayerKind.Neutral
                || game.botAssignments |> Map.containsKey p.id)
            |> List.length
        if participantCount <= 1
        then raise <| GameConfigurationException("Cannot start game with only one player.")
        elif game.status <> GameStatus.Pending
        then raise <| GameConfigurationException("Cannot start game unless it is pending.") 
        else
            task {
                let! addNeutralPlayerEffects = playerServ.fillEmptyPlayerSlots game

                //Order is important. Players must exist before they can be given pieces
                let e1 = 
                    match addNeutralPlayerEffects.Length with
                    | 0 -> 
                        None
                    | _ ->
                        Some {
                            kind = EventKind.PlayerJoined
                            effects = addNeutralPlayerEffects
                            createdByUserId = session.user.id
                            actingPlayerId = Context.getActingPlayerId session game                    
                        }
                
                let e2 = {
                    kind = EventKind.GameStarted
                    effects = [
                        Effect.GameStatusChanged { 
                            oldValue = GameStatus.Pending
                            newValue = GameStatus.InProgress 
                        }
                    ]
                    createdByUserId = session.user.id
                    actingPlayerId = Context.getActingPlayerId session game
                }

                return (e1, e2)                
            }

    member private x.isDuelMode(players : Player list) : bool =
        let uniqueUserIds =
            players
            |> List.choose (fun p -> p.userId)
            |> List.distinct
        players.Length = 4 && uniqueUserIds.Length = 2

    member x.assignStartingConditions(players : Player list) : Player list =
        if x.isDuelMode players then
            x.assignDuelStartingConditions players
        else
            x.assignNormalStartingConditions players

    member private x.assignDuelStartingConditions(players : Player list) : Player list =
        // 2-player duel: each user gets 2 opposite regions with the same color
        let twoColors = [0..(Constants.maxRegions-1)] |> List.shuffle |> Seq.take 2 |> Seq.toList
        // Regions 0,2 are opposite; 1,3 are opposite on a 4-region board
        let regionPairs = if Random().Next(2) = 0 then ([0; 2], [1; 3]) else ([1; 3], [0; 2])

        let grouped = players |> List.groupBy (fun p -> p.userId)
        let userA = grouped.[0] |> snd
        let userB = grouped.[1] |> snd

        let assignPair (pair : Player list) (color : int) (regions : int list) =
            pair |> List.mapi (fun i p ->
                { p with startingRegion = Some regions.[i]; colorId = Some color }
            )

        let assigned =
            (assignPair userA twoColors.[0] (fst regionPairs))
            @ (assignPair userB twoColors.[1] (snd regionPairs))

        let dict = Enumerable.ToDictionary (assigned, (fun p -> p.name))

        // Interleave turns: A1, B1, A2, B2
        let aSlots = assignPair userA twoColors.[0] (fst regionPairs)
        let bSlots = assignPair userB twoColors.[1] (snd regionPairs)
        let interleaved = [aSlots.[0]; bSlots.[0]; aSlots.[1]; bSlots.[1]]
        for (i, p) in interleaved |> Seq.mapi (fun i p -> (i, p)) do
            dict.[p.name] <- { dict.[p.name] with startingTurnNumber = Some i }

        dict.Values
        |> Seq.map (fun p -> { p with status = PlayerStatus.Alive })
        |> Seq.toList

    member private x.assignNormalStartingConditions(players : Player list) : Player list =
        let colorIds = [0..(Constants.maxRegions-1)] |> List.shuffle |> Seq.take players.Length
        let regions = [0..(players.Length-1)] |> List.shuffle

        let playersWithAssignments =
            players
            |> Seq.zip3 colorIds regions
            |> Seq.map (fun (c, r, p) ->
                {
                    p with
                        startingRegion = Some r
                        startingTurnNumber = None
                        colorId = Some c
                }
            )

        (*
            At this point, neutral players may still have ID = 0, because
            they have not yet been persisted as part of the StartGame transaction.
            Index on name instead of id.
        *)
        let dict = Enumerable.ToDictionary (playersWithAssignments, (fun p -> p.name))

        let allPlayers =
            players
            |> List.shuffle
            |> Seq.mapi (fun i p -> (i, p))

        for (i, p) in allPlayers do
            dict.[p.name] <- { dict.[p.name] with startingTurnNumber = Some i }

        dict.Values
        |> Seq.map (fun p ->
            let status = PlayerStatus.Alive
            { p with status = status }
        )
        |> Seq.toList

    member x.createPieces(board : BoardMetadata, players : Player list, rulesetKind : RulesetKind) : Piece list =
        match rulesetKind with
        | RulesetKind.Classic -> Rulesets.ClassicPieceLayout.createPieces(board, players)
        | _ -> Rulesets.TotalWarPieceLayout.createPieces(board, players)

    member x.applyStartGame (game : Game) : Game =
        let board = BoardModelUtility.getBoardMetadata game.parameters.regionCount
        let players = x.assignStartingConditions game.players

        let mutable pieces = x.createPieces(board, players, game.parameters.rulesetKind)

        // In duel mode, replace one Conduit per user with a Corpse
        if x.isDuelMode players then
            let grouped = players |> List.groupBy (fun p -> p.userId)
            for (_, userPlayers) in grouped do
                // Each user has 2 player slots, each with a Conduit. Replace the second one.
                let secondSlot = userPlayers.[1]
                pieces <- pieces |> List.map (fun piece ->
                    if piece.playerId = Some secondSlot.id && piece.kind = PieceKind.Conduit
                    then { piece with kind = PieceKind.Corpse; playerId = None }
                    else piece
                )

        let game =
            {
                game with
                    status = GameStatus.InProgress
                    pieces = pieces
                    players = players
                    turnCycle = players //Starting conditions must first be assigned
                        |> List.filter (fun p -> p.startingTurnNumber.IsSome)
                        |> List.sortBy (fun p -> p.startingTurnNumber.Value)
                        |> List.map (fun p -> p.id)
                    currentTurn = Some Turn.empty
            }

        let options = (selectionOptionsServ.getSelectableCellsFromState game)
        let turn = { game.currentTurn.Value with selectionOptions = options; turnStartedAt = Some DateTime.UtcNow }
        { game with  currentTurn =  Some turn }