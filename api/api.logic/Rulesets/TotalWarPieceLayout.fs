module Djambi.Api.Logic.Rulesets.TotalWarPieceLayout

open Djambi.Api.Common
open Djambi.Api.Logic.ModelExtensions.BoardModelExtensions
open Djambi.Api.Model
open Djambi.Api.Enums

let createPlayerPieces(board : BoardMetadata, player : Player, startingId : int) : Piece list =
    let getPiece(id : int, pieceType: PieceKind, x : int, y : int) =
        {
            id = id
            kind = pieceType
            playerId = Some player.id
            originalPlayerId = player.id
            cellId = board.cellAt({x = x; y = y; region = player.startingRegion.Value}).id
        }
    let n = Constants.regionSize - 1
    [
        getPiece(startingId, PieceKind.Conduit, n,n)
        getPiece(startingId+1, PieceKind.Scientist, n,n-1)
        getPiece(startingId+2, PieceKind.Hunter, n-1,n)
        getPiece(startingId+3, PieceKind.Diplomat, n-1,n-1)
        getPiece(startingId+4, PieceKind.Reaper, n-2,n-2)
        getPiece(startingId+5, PieceKind.Thug, n-2,n-1)
        getPiece(startingId+6, PieceKind.Thug, n-2,n)
        getPiece(startingId+7, PieceKind.Thug, n-1,n-2)
        getPiece(startingId+8, PieceKind.Thug, n,n-2)
    ]

let createPieces(board : BoardMetadata, players : Player list) : Piece list =
    players
    |> List.mapi (fun i cond -> createPlayerPieces(board, cond, i*Constants.piecesPerPlayer))
    |> List.collect id
