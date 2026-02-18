namespace Djambi.Api.Logic

open System
open Djambi.Api.Model.GameModel

type PieceStrategy() =
    abstract member moveMaxDistance : int
    abstract member canTargetWithMove : bool
    abstract member canTargetAfterMove : bool
    abstract member canTargetPiece : Piece -> Piece -> bool
    abstract member canStayInCenter : bool
    abstract member canEnterCenterToEvictPiece : bool
    abstract member canDropTarget : bool
    abstract member movesTargetToOrigin : bool
    abstract member killsTarget : bool
    abstract member killsControllingPlayerWhenKilled : bool
    abstract member isAlive : bool

    default x.moveMaxDistance = Int32.MaxValue
    default x.canTargetWithMove = false
    default x.canTargetAfterMove = false
    default x.canTargetPiece (subject : Piece) (target : Piece) = false
    default x.canStayInCenter = false
    default x.canEnterCenterToEvictPiece = false
    default x.canDropTarget = false
    default x.movesTargetToOrigin = false
    default x.killsTarget = false
    default x.killsControllingPlayerWhenKilled = false
    default x.isAlive = true
