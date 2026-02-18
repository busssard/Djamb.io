namespace Djambi.Api.Logic

open System.Collections.Generic
open Djambi.Api.Model.GameModel
open Djambi.Api.Enums

type TotalWarHunterStrategy() =
    inherit PieceStrategy() with
        override x.canTargetWithMove = true
        override x.canTargetPiece (subject : Piece) (target : Piece) =
            target.kind <> PieceKind.Corpse &&
            target.playerId <> subject.playerId
        override x.canEnterCenterToEvictPiece = true
        override x.movesTargetToOrigin = true
        override x.killsTarget = true

type TotalWarConduitStrategy() =
    inherit PieceStrategy() with
        override x.canTargetWithMove = true
        override x.canTargetPiece (subject : Piece) (target : Piece) =
            target.kind <> PieceKind.Corpse &&
            target.playerId <> subject.playerId
        override x.canStayInCenter = true
        override x.canEnterCenterToEvictPiece = true
        override x.canDropTarget = true
        override x.killsTarget = true
        override x.killsControllingPlayerWhenKilled = true

type TotalWarCorpseStrategy() =
    inherit PieceStrategy()
        override x.isAlive = false

type TotalWarDiplomatStrategy() =
    inherit PieceStrategy() with
        override x.canTargetWithMove = true
        override x.canTargetPiece (subject : Piece) (target : Piece) =
            target.kind <> PieceKind.Corpse &&
            target.playerId <> subject.playerId
        override x.canEnterCenterToEvictPiece = true
        override x.canDropTarget = true

type TotalWarReaperStrategy() =
    inherit PieceStrategy() with
        override x.canTargetWithMove = true
        override x.canTargetPiece (subject : Piece) (target : Piece) =
            target.kind = PieceKind.Corpse
        override x.canEnterCenterToEvictPiece = true
        override x.canDropTarget = true

type TotalWarScientistStrategy() =
    inherit PieceStrategy() with
        override x.canTargetAfterMove = true
        override x.canTargetPiece (subject : Piece) (target : Piece) =
            target.kind <> PieceKind.Corpse &&
            target.playerId <> subject.playerId
        override x.killsTarget = true

type TotalWarThugStrategy() =
    inherit PieceStrategy() with
        override x.moveMaxDistance = 2
        override x.canTargetWithMove = true
        override x.canTargetPiece (subject : Piece) (target : Piece) =
            target.kind <> PieceKind.Corpse &&
            target.playerId <> subject.playerId
        override x.canDropTarget = true
        override x.killsTarget = true

module TotalWarPieces =

    let private strategies = Dictionary<PieceKind, PieceStrategy>()
    strategies.Add(PieceKind.Hunter, TotalWarHunterStrategy())
    strategies.Add(PieceKind.Conduit, TotalWarConduitStrategy())
    strategies.Add(PieceKind.Corpse, TotalWarCorpseStrategy())
    strategies.Add(PieceKind.Diplomat, TotalWarDiplomatStrategy())
    strategies.Add(PieceKind.Reaper, TotalWarReaperStrategy())
    strategies.Add(PieceKind.Scientist, TotalWarScientistStrategy())
    strategies.Add(PieceKind.Thug, TotalWarThugStrategy())

    let getStrategy (piece : Piece) : PieceStrategy =
        strategies.[piece.kind]

    let getStrategyForKind (kind : PieceKind) : PieceStrategy =
        strategies.[kind]
