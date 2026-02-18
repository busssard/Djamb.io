namespace Djambi.Api.Logic

open System.Collections.Generic
open Djambi.Api.Model.GameModel
open Djambi.Api.Enums

type ClassicHunterStrategy() =
    inherit PieceStrategy() with
        override x.canTargetWithMove = true
        override x.canTargetPiece (subject : Piece) (target : Piece) =
            target.kind <> PieceKind.Corpse &&
            target.playerId <> subject.playerId
        override x.canEnterCenterToEvictPiece = true
        override x.movesTargetToOrigin = true
        override x.killsTarget = true

type ClassicConduitStrategy() =
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

type ClassicCorpseStrategy() =
    inherit PieceStrategy()
        override x.isAlive = false

type ClassicDiplomatStrategy() =
    inherit PieceStrategy() with
        override x.canTargetWithMove = true
        override x.canTargetPiece (subject : Piece) (target : Piece) =
            target.kind <> PieceKind.Corpse &&
            target.playerId <> subject.playerId
        override x.canEnterCenterToEvictPiece = true
        override x.canDropTarget = true

type ClassicReaperStrategy() =
    inherit PieceStrategy() with
        override x.canTargetWithMove = true
        override x.canTargetPiece (subject : Piece) (target : Piece) =
            target.kind = PieceKind.Corpse
        override x.canEnterCenterToEvictPiece = true
        override x.canDropTarget = true

type ClassicScientistStrategy() =
    inherit PieceStrategy() with
        override x.canTargetAfterMove = true
        override x.canTargetPiece (subject : Piece) (target : Piece) =
            target.kind <> PieceKind.Corpse &&
            target.playerId <> subject.playerId
        override x.killsTarget = true

type ClassicThugStrategy() =
    inherit PieceStrategy() with
        override x.moveMaxDistance = 2
        override x.canTargetWithMove = true
        override x.canTargetPiece (subject : Piece) (target : Piece) =
            target.kind <> PieceKind.Corpse &&
            target.playerId <> subject.playerId
        override x.canDropTarget = true
        override x.killsTarget = true

module ClassicPieces =

    let private strategies = Dictionary<PieceKind, PieceStrategy>()
    strategies.Add(PieceKind.Hunter, ClassicHunterStrategy())
    strategies.Add(PieceKind.Conduit, ClassicConduitStrategy())
    strategies.Add(PieceKind.Corpse, ClassicCorpseStrategy())
    strategies.Add(PieceKind.Diplomat, ClassicDiplomatStrategy())
    strategies.Add(PieceKind.Reaper, ClassicReaperStrategy())
    strategies.Add(PieceKind.Scientist, ClassicScientistStrategy())
    strategies.Add(PieceKind.Thug, ClassicThugStrategy())

    let getStrategy (piece : Piece) : PieceStrategy =
        strategies.[piece.kind]

    let getStrategyForKind (kind : PieceKind) : PieceStrategy =
        strategies.[kind]
