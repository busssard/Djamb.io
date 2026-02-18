namespace Djambi.Api.Logic

open Djambi.Api.Model.GameModel
open Djambi.Api.Enums

module Pieces =

    let getStrategy (rulesetKind : RulesetKind) (piece : Piece) : PieceStrategy =
        match rulesetKind with
        | RulesetKind.Classic -> ClassicPieces.getStrategy piece
        | _ -> TotalWarPieces.getStrategy piece

    let getStrategyForKind (rulesetKind : RulesetKind) (kind : PieceKind) : PieceStrategy =
        match rulesetKind with
        | RulesetKind.Classic -> ClassicPieces.getStrategyForKind kind
        | _ -> TotalWarPieces.getStrategyForKind kind
