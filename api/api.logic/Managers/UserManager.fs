namespace Djambi.Api.Logic.Managers

open System
open System.ComponentModel.DataAnnotations
open Djambi.Api.Common.Control
open Djambi.Api.Db.Interfaces
open Djambi.Api.Enums
open Djambi.Api.Logic
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Model

type UserManager(encyptionService: IEncryptionService,
                 userRepo : IUserRepository,
                 sessionService : ISessionService) =
    interface IUserManager with
        member __.createUser request sessionOption =
            match sessionOption with
            | Some s when not (s.user.has Privilege.EditUsers) ->
                raise <| UnauthorizedAccessException("Cannot create user if logged in.")
            | _ ->
                task {
                    let request =
                        match request.password with
                        | Some password ->
                            if password.Contains(request.name)
                            then raise <| ValidationException("Password cannot contain username.")
                            elif request.name.Contains(password)
                            then raise <| ValidationException("Username cannot contain password.")
                            let hash = encyptionService.hash password
                            { request with password = Some hash }
                        | None -> request
                    let! user = userRepo.createUser request
                    return user |> UserDetails.hideDetails
                }

        member __.quickRegister name email =
            task {
                // Check if user already exists
                match! userRepo.getUserByName name with
                | Some existing when existing.password.IsNone ->
                    // Passwordless user exists — sign them back in
                    return! sessionService.createSessionForUser existing.id
                | Some _ ->
                    // User exists but has a password — can't quick-join
                    return raise <| ValidationException("User name taken.")
                | None ->
                    // New user — create and sign in
                    let request : CreateUserRequest =
                        {
                            name = name
                            password = None
                            email = email
                        }
                    let! user = userRepo.createUser request
                    return! sessionService.createSessionForUser user.id
            }

        member __.deleteUser userId session =
            Security.ensureSelfOrHas Privilege.EditUsers session userId
            userRepo.deleteUser userId

        member __.getUser userId session =
            Security.ensureSelfOrHas Privilege.EditUsers session userId
            task {
                match! userRepo.getUser userId with
                | None -> return raise <| NotFoundException("User not found.")
                | Some user -> return user |> UserDetails.hideDetails
            }
            
        member x.getCurrentUser session =
            (x :> IUserManager).getUser session.user.id session