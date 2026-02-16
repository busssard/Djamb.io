namespace Djambi.Api.Logic.Services

open System
open Djambi.Api.Common.Control
open Djambi.Api.Db.Interfaces
open Djambi.Api.Model
open Djambi.Api.Logic.Interfaces
open System.Threading.Tasks
open System.Security.Authentication

type SessionService(encryptionService : IEncryptionService,
                    sessionRepo : ISessionRepository,
                    userRepo : IUserRepository,
                    magicLinkRepo : IMagicLinkRepository,
                    emailService : IEmailService) =

    let maxFailedLoginAttempts = 5
    let accountLockTimeout = TimeSpan.FromHours(1.0)
    let sessionTimeout = TimeSpan.FromDays(7.0)
    let magicLinkTimeout = TimeSpan.FromMinutes(15.0)

    interface ISessionService with
        member __.openSession request = 
            let isWithinLockTimeoutPeriod (u : UserDetails) =
                u.lastFailedLoginAttemptOn.IsNone
                || DateTime.UtcNow - u.lastFailedLoginAttemptOn.Value < accountLockTimeout

            let errorIfLocked (user : UserDetails) =
                if user.failedLoginAttempts >= maxFailedLoginAttempts
                    && isWithinLockTimeoutPeriod user
                then raise <| AuthenticationException("Account locked.")
                else ()

            let errorIfInvalidPassword (user : UserDetails) =
                match user.password with
                | None ->
                    // Passwordless user — cannot log in via password
                    raise <| AuthenticationException("This account uses email sign-in. Use the magic link option.")
                    Task.FromResult ()
                | Some hash ->
                    let result = encryptionService.check (hash, request.password)
                    if result.verified
                    then Task.FromResult ()
                    else
                        let attempts =
                            if isWithinLockTimeoutPeriod user
                            then user.failedLoginAttempts + 1
                            else 1

                        let request = UpdateFailedLoginsRequest.increment (user.id, attempts)
                        task {
                            let! _ = userRepo.updateFailedLoginAttempts request
                            raise <| AuthenticationException("Incorrect password.")
                        }

            let deleteSessionForUser (userId : int) : Task<unit> =            
                task {
                    match! sessionRepo.getSession (SessionQuery.byUserId userId) with
                    | None -> return ()
                    | Some session -> return! sessionRepo.deleteSession session.token
                }

            task {
                match! userRepo.getUserByName request.username with
                | None -> return raise <| AuthenticationException("User does not exist.")
                | Some user ->
                    errorIfLocked user
                    let! _ = errorIfInvalidPassword user

                    //If a session already exists for this user, delete it
                    let! _ = deleteSessionForUser user.id

                    //Create a new session            
                    let request : CreateSessionRequest =
                        {
                            userId = user.id
                            token = Guid.NewGuid().ToString()
                            expiresOn = DateTime.UtcNow.Add(sessionTimeout)
                        }
                    let! session = sessionRepo.createSession request
                    let! _ = userRepo.updateFailedLoginAttempts (UpdateFailedLoginsRequest.reset user.id)

                    return session
            }

        member __.createSessionForUser userId =
            task {
                // Delete any existing session for this user
                match! sessionRepo.getSession (SessionQuery.byUserId userId) with
                | Some existing -> do! sessionRepo.deleteSession existing.token
                | None -> ()

                let request : CreateSessionRequest =
                    {
                        userId = userId
                        token = Guid.NewGuid().ToString()
                        expiresOn = DateTime.UtcNow.Add(sessionTimeout)
                    }
                return! sessionRepo.createSession request
            }

        member __.requestMagicLink email baseUrl =
            task {
                // Always return success to prevent email enumeration
                match! userRepo.getUserByEmail email with
                | None -> return ()
                | Some user ->
                    let token = Guid.NewGuid().ToString("N")
                    let expiresOn = DateTime.UtcNow.Add(magicLinkTimeout)
                    let! _ = magicLinkRepo.createToken user.id email token expiresOn
                    let link = sprintf "%s/auth/verify/%s" baseUrl token
                    do! emailService.sendMagicLink email link
            }

        member __.verifyMagicLink token =
            task {
                let! magicLinkOpt = magicLinkRepo.getByToken token
                let magicLink =
                    match magicLinkOpt with
                    | None -> raise <| AuthenticationException("Invalid or expired link.")
                    | Some ml -> ml

                if magicLink.usedOn.IsSome then
                    raise <| AuthenticationException("This link has already been used.")
                if DateTime.UtcNow > magicLink.expiresOn then
                    raise <| AuthenticationException("This link has expired.")

                do! magicLinkRepo.markUsed magicLink.id

                // Delete any existing session and create a new one
                match! sessionRepo.getSession (SessionQuery.byUserId magicLink.userId) with
                | Some existing -> do! sessionRepo.deleteSession existing.token
                | None -> ()

                let request : CreateSessionRequest =
                    {
                        userId = magicLink.userId
                        token = Guid.NewGuid().ToString()
                        expiresOn = DateTime.UtcNow.Add(sessionTimeout)
                    }
                return! sessionRepo.createSession request
            }

        member __.closeSession session =
            sessionRepo.deleteSession session.token

        member __.getAndRenewSession token =
            task {
                match! sessionRepo.getSession (SessionQuery.byToken token) with
                | None -> return None
                | Some session ->
                    if session.expiresOn <= DateTime.UtcNow
                    then
                        let! _ = sessionRepo.deleteSession session.token
                        return None
                    else
                        let! session = sessionRepo.renewSessionExpiration(session.id, DateTime.UtcNow.Add(sessionTimeout))
                        return Some session          
            }