[<AutoOpen>]
module Djambi.Api.Model.UserModel

open System
open Djambi.Api.Enums

type User =
    {
        id : int
        name : string
        email : string option
        privileges : Privilege list
    }

type User with
    member x.has (p : Privilege) =
        x.privileges |> List.contains p

type UserDetails =
    {
        id : int
        name : string
        email : string option
        privileges : Privilege list
        password : string option
        failedLoginAttempts : int
        lastFailedLoginAttemptOn : DateTime option
    }

module UserDetails =
    let hideDetails (user : UserDetails) : User =
        {
            id = user.id
            name = user.name
            email = user.email
            privileges = user.privileges
        }

type CreateUserRequest =
    {
        name : string
        password : string option
        email : string option
    }

type CreationSource =
    {
        userId : int
        userName : string
        time : DateTime
    }

type PasswordCheckResult = {
    verified: bool
    needsUpgrade: bool
}
