[<AutoOpen>]
module Djambi.Api.Model.SessionModel

open System
open Djambi.Api.Model

type Session =
    {
        id : int
        user : User
        token : string
        createdOn : DateTime
        expiresOn : DateTime
    }

type LoginRequest =
    {
        username : string
        password : string
    }

type MagicLinkRequest =
    {
        email : string
    }

type MagicLinkToken =
    {
        id : int
        token : string
        userId : int
        email : string
        createdOn : DateTime
        expiresOn : DateTime
        usedOn : DateTime option
    }