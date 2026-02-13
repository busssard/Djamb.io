namespace Djambi.Api.Logic.Bots

open System
open System.Threading.Tasks
open Djambi.Api.Model
open Djambi.Api.Logic.Interfaces

/// Reference implementation of IBotPlayer.
/// Picks uniformly at random from the legal options each selection phase.
/// Use this as a starting point for writing your own bot.
///
/// Strategy: None — pure random. Useful as a baseline for measuring
/// how much smarter other bots are.
type RandomBot() =
    let rng = Random()

    interface IBotPlayer with
        member __.info =
            {
                name = "random"
                description = "Picks a random legal move each turn. Simple but unpredictable."
            }

        /// Selects a random cell from game.currentTurn.selectionOptions.
        /// Ignores playerId since the strategy doesn't depend on which player we are.
        member __.selectCell (game : Game) (_playerId : int) : Task<int> =
            match game.currentTurn with
            | None ->
                raise (InvalidOperationException("No active turn"))
            | Some turn ->
                match turn.selectionOptions with
                | [] ->
                    raise (InvalidOperationException("No selection options available"))
                | options ->
                    let index = rng.Next(options.Length)
                    Task.FromResult(options.[index])
