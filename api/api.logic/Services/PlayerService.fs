namespace Djambi.Api.Logic.Services

open System
open System.ComponentModel
open System.Data
open System.Linq
open System.Threading.Tasks
open Djambi.Api.Common.Collections
open Djambi.Api.Common.Control
open Djambi.Api.Db.Interfaces
open Djambi.Api.Enums
open Djambi.Api.Logic
open Djambi.Api.Model

type PlayerService(gameRepo : IGameRepository) =
    member __.getAddPlayerEvent (game : Game, request : CreatePlayerRequest) (session : Session) : CreateEventRequest =
        let self = session.user
        if game.status <> GameStatus.Pending
        then raise <| GameConfigurationException("Can only add players to pending games.")
        elif request.name.IsSome
            && game.players |> List.exists (fun p -> String.Equals(p.name, request.name.Value, StringComparison.OrdinalIgnoreCase))
        then raise <| DuplicateNameException("Player name taken.")
        elif game.players.Length >= game.parameters.regionCount
        then raise <| GameConfigurationException("Max player count reached.")
          else
            match request.kind with
            | PlayerKind.User ->
                if request.userId.IsNone
                then raise <| GameConfigurationException("UserID must be provided when adding a user player.")
                elif request.name.IsSome
                then raise <| GameConfigurationException("Cannot provide name when adding a user player.")
                elif game.players |> List.exists (fun p -> p.kind = PlayerKind.User && p.userId = request.userId)
                then raise <| GameConfigurationException("User is already a player.")
                elif not (self.has Privilege.EditPendingGames) && request.userId.Value <> self.id
                then raise <| GameConfigurationException("Cannot add other users to a game.")
                else ()

            | PlayerKind.Guest ->
                if not game.parameters.allowGuests
                then raise <| GameConfigurationException("Game does not allow guest players.")
                elif request.userId.IsNone
                then raise <| GameConfigurationException("UserID must be provided when adding a guest player.")
                elif request.name.IsNone
                then raise <| GameConfigurationException("Must provide name when adding a guest player.")
                elif not (self.has Privilege.EditPendingGames) && request.userId.Value <> self.id
                then raise <| GameConfigurationException("Cannot add guests for other users to a game.")
                else ()

            | PlayerKind.Neutral ->
                match request.botName with
                | Some _ ->
                    if request.name.IsNone
                    then raise <| GameConfigurationException("Must provide name when adding a bot player.")
                    elif not (self.has Privilege.EditPendingGames)
                         && game.createdBy.userId <> self.id
                         && not (game.players |> List.exists (fun p -> p.kind = PlayerKind.User && p.userId = Some self.id))
                    then raise <| GameConfigurationException("Must be a player or game creator to add bots.")
                    else ()
                | None ->
                    raise <| GameConfigurationException("Cannot directly add neutral players to a game.")
            | _ -> raise <| InvalidEnumArgumentException()

        match request.botName with
        | Some _ ->
            // Bot player: use NeutralPlayerAdded effect (no userId)
            let placeholderPlayerId = -(game.players.Length + 1)
            let f : NeutralPlayerAddedEffect =
                {
                    name = request.name.Value
                    placeholderPlayerId = placeholderPlayerId
                }
            {
                kind = EventKind.PlayerJoined
                effects = [ Effect.NeutralPlayerAdded f ]
                createdByUserId = self.id
                actingPlayerId = None
            }
        | None ->
            {
                kind = EventKind.PlayerJoined
                effects = [ PlayerAddedEffect.fromRequest request ]
                createdByUserId = self.id
                actingPlayerId = None
            }

    member __.getRemovePlayerEvent (game : Game, playerId : int) (session : Session) : CreateEventRequest =
        let self = session.user
        if game.status <> GameStatus.Pending then
            raise <| GameConfigurationException("Cannot remove players unless game is Pending.")
        else
            match game.players |> List.tryFind (fun p -> p.id = playerId) with
            | None -> raise <| NotFoundException("Player not found.")
            | Some player ->
                // Bot neutrals (in botAssignments) can be removed by the game creator
                let isBotPlayer = game.botAssignments |> Map.containsKey player.id
                if player.kind = PlayerKind.Neutral && isBotPlayer then
                    if not (self.has Privilege.EditPendingGames || game.createdBy.userId = self.id)
                    then raise <| GameConfigurationException("Only the game creator can remove bot players.")
                    else
                        let effects = [ Effect.PlayerRemoved { oldPlayer = player } ]
                        {
                            kind = EventKind.PlayerRemoved
                            effects = effects
                            createdByUserId = self.id
                            actingPlayerId = Context.getActingPlayerId session game
                        }
                else
                    match player.userId with
                    | None -> raise <| GameConfigurationException("Cannot remove neutral players from game.")
                    | Some x ->
                        if not <| (self.has Privilege.EditPendingGames
                            || game.createdBy.userId = self.id
                            || x = self.id)
                        then raise <| GameConfigurationException("Cannot remove other users from game.")
                        else
                            let effects = new ArrayList<Effect>()

                            let playersToRemove =
                                match player.kind with
                                | PlayerKind.User ->
                                    game.players
                                    |> List.filter (fun p -> p.userId = player.userId)
                                | PlayerKind.Guest ->
                                    game.players
                                    |> List.filter (fun p -> p.id = playerId)
                                | _ -> [] //Already eliminated this case in validation above

                            for p in playersToRemove do
                                effects.Add(Effect.PlayerRemoved { oldPlayer = p })

                            //Cancel game if creator quit
                            if game.createdBy.userId = player.userId.Value
                                && player.kind = PlayerKind.User
                            then
                                effects.Add(Effect.GameStatusChanged { oldValue = GameStatus.Pending; newValue = GameStatus.Canceled })
                            else ()

                            {
                                kind = EventKind.PlayerRemoved
                                effects = effects |> Seq.toList
                                createdByUserId = self.id
                                actingPlayerId = Context.getActingPlayerId session game
                            }

    member __.fillEmptyPlayerSlots (game : Game) : Task<list<Effect>> =
        let missingPlayerCount = game.parameters.regionCount - game.players.Length
        let nonNeutralPlayers = game.players |> List.filter (fun p -> p.kind <> PlayerKind.Neutral)

        // 2-player duel mode: 4-region board with exactly 2 users → give each user a second slot
        if missingPlayerCount = 2 && game.parameters.regionCount = 4 && nonNeutralPlayers.Length = 2 then
            let effects =
                nonNeutralPlayers
                |> List.mapi (fun i p ->
                    let f : PlayerAddedEffect =
                        {
                            name = Some (p.name + " (2)")
                            userId = p.userId.Value
                            kind = PlayerKind.User
                        }
                    Effect.PlayerAdded f
                )
            Task.FromResult effects
        else

        let getNeutralPlayerNamesToUse (possibleNames : string list) =
            Enumerable.Except(
                possibleNames,
                game.players |> Seq.map (fun p -> p.name),
                StringComparer.OrdinalIgnoreCase)
            |> List.ofSeq
            |> List.shuffle
            |> Seq.take missingPlayerCount

        if missingPlayerCount = 0
        then Task.FromResult []
        else
            task {
                let! names = gameRepo.getNeutralPlayerNames()
                let namesToUse = getNeutralPlayerNamesToUse names
                let effects = namesToUse |> Seq.mapi (fun i name ->
                    let placeholderPlayerId = -(i+1) //Use negative numbers to avoid conflicts
                    let f : NeutralPlayerAddedEffect =
                        {
                            name = name
                            placeholderPlayerId = placeholderPlayerId
                        }
                    Effect.NeutralPlayerAdded f
                )
                return effects |> Seq.toList
            }