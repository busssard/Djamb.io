namespace Djambi.Api.Logic.Interfaces

open System.Threading.Tasks
open Djambi.Api.Model

/// Metadata describing a bot player, used for discovery and display.
type BotInfo =
    {
        /// Unique identifier for the bot. Used in API routes: GET /api/bots/{name}
        /// Must be URL-safe (lowercase alphanumeric + hyphens). Example: "random", "greedy-tactician"
        name : string

        /// Human-readable description of the bot's strategy. Displayed to players
        /// when choosing opponents. Keep under 200 characters.
        description : string
    }

/// Interface for implementing a bot player in Djambi-N.
///
/// ## How Djambi Turns Work
///
/// A turn consists of one or more sequential cell selections. The game tracks
/// which cells are valid for the next selection in `game.currentTurn.selectionOptions`.
///
/// The selection sequence depends on the piece type:
///   1. **Subject** — Select which of your pieces to move (from `selectionOptions`)
///   2. **Move** — Select where to move it (from updated `selectionOptions`)
///   3. **Target** (optional) — Select an enemy piece to interact with
///   4. **Drop** (optional) — Select where to place a captured/displaced piece
///   5. **Vacate** (optional) — If moving to center, select where to relocate the occupant
///
/// After all required selections, the turn status becomes `AwaitingCommit`.
///
/// ## What Your Bot Receives
///
/// `selectCell` is called once per selection phase. Each call receives the full
/// current `Game` state, which includes:
///   - `game.currentTurn.selectionOptions : int list` — **the valid cell IDs to choose from**
///   - `game.currentTurn.requiredSelectionKind` — what type of selection this is (Subject, Move, etc.)
///   - `game.currentTurn.selections` — selections already made this turn
///   - `game.currentTurn.status` — AwaitingSelection, AwaitingCommit, or DeadEnd
///   - `game.pieces` — all pieces on the board with positions, types, and ownership
///   - `game.players` — all players with status (Alive, Eliminated, etc.)
///   - `game.turnCycle` — turn order (Head = current player)
///   - `game.parameters.regionCount` — board size (3-8 player configurations)
///
/// ## What Your Bot Returns
///
/// Return a single `int` — the cell ID to select. This must be one of the values
/// in `game.currentTurn.selectionOptions`. The server validates the choice and
/// advances the turn state.
///
/// ## Minimal Example (F#)
///
///     type MyBot() =
///         let rng = System.Random()
///         interface IBotPlayer with
///             member __.info = { name = "my-bot"; description = "My custom bot" }
///             member __.selectCell (game: Game) (_playerId: int) =
///                 let options = game.currentTurn.Value.selectionOptions
///                 let pick = options.[rng.Next(options.Length)]
///                 Task.FromResult(pick)
///
/// ## Registration
///
/// Register your bot in Program.fs DI setup:
///
///     builder.Services.AddSingleton<IBotRegistry>(fun _ ->
///         let registry = BotRegistry() :> IBotRegistry
///         registry.register(MyBot())
///         registry)
///
/// ## Key Types Reference
///
/// **Game** (`api.model/GameModel.fs`):
///   - `pieces : Piece list` — id, kind (PieceKind enum), playerId, cellId
///   - `players : Player list` — id, name, status (PlayerStatus enum), kind (PlayerKind enum)
///   - `turnCycle : int list` — ordered player IDs; Head is current player
///   - `currentTurn : Turn option` — active turn with selections and options
///
/// **Turn** (`api.model/GameModel.fs`):
///   - `status : TurnStatus` — AwaitingSelection | AwaitingCommit | DeadEnd
///   - `selectionOptions : int list` — valid cell IDs for next selection
///   - `requiredSelectionKind : SelectionKind option` — Subject | Move | Target | Drop | Vacate
///   - `selections : Selection list` — selections made so far this turn
///
/// **PieceKind** (`api.enums/Enums.fs`):
///   Chief (1), Assassin (2), Reporter (3), Diplomat (4), Gravedigger (5), Thug (6), Conduit (7)
///   — Original Djambi names mapped to neutral piece types
///
/// **PlayerStatus** (`api.enums/Enums.fs`):
///   Pending, Alive, Eliminated, Conceded, WillConcede, AcceptsDraw, Victorious
type IBotPlayer =
    /// Bot metadata for API discovery and player-facing display.
    abstract member info : BotInfo

    /// Called once per selection phase during a bot's turn.
    ///
    /// Parameters:
    ///   game — Full game state including board, pieces, players, and current turn
    ///   playerId — The bot's player ID in this game (matches an entry in game.players)
    ///
    /// Returns: A cell ID from game.currentTurn.selectionOptions.
    ///
    /// This method is called multiple times per turn (once for each selection phase).
    /// For example, a simple move requires two calls: one for Subject (pick piece),
    /// one for Move (pick destination). Complex moves may require 3-5 calls.
    ///
    /// The method may be async (return Task) for bots that call external services
    /// (e.g., ML inference endpoints). Synchronous bots can use Task.FromResult.
    abstract member selectCell : game:Game -> playerId:int -> Task<int>

/// Registry for discovering and managing available bot players.
/// The server creates a single IBotRegistry at startup and registers all bots.
/// The BotController uses this to serve GET /api/bots and POST /api/bots/{name}/select-cell.
type IBotRegistry =
    /// Register a new bot. If a bot with the same name exists, it is replaced.
    abstract member register : bot:IBotPlayer -> unit

    /// Look up a bot by name. Returns None if not found.
    abstract member getBot : name:string -> IBotPlayer option

    /// List all registered bots' metadata.
    abstract member listBots : unit -> BotInfo list
