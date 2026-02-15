namespace Djambi.Api.Db.Repositories

open System
open FSharp.Control.Tasks
open Microsoft.EntityFrameworkCore
open Djambi.Api.Db.Interfaces
open Djambi.Api.Db.Model
open Djambi.Api.Model

type MagicLinkRepository(context : DjambiDbContext) =

    let toMagicLinkToken (source : MagicLinkSqlModel) : MagicLinkToken =
        {
            id = source.MagicLinkId
            token = source.Token
            userId = source.UserId
            email = source.Email
            createdOn = source.CreatedOn
            expiresOn = source.ExpiresOn
            usedOn = source.UsedOn |> Option.ofNullable
        }

    interface IMagicLinkRepository with
        member __.createToken userId email token expiresOn =
            task {
                let m = MagicLinkSqlModel()
                m.Token <- token
                m.UserId <- userId
                m.Email <- email
                m.CreatedOn <- DateTime.UtcNow
                m.ExpiresOn <- expiresOn
                let! _ = context.MagicLinks.AddAsync(m)
                let! _ = context.SaveChangesAsync()
                return m |> toMagicLinkToken
            }

        member __.getByToken token =
            task {
                match! context.MagicLinks.SingleOrDefaultAsync(fun m -> m.Token = token) with
                | null -> return None
                | m -> return Some (m |> toMagicLinkToken)
            }

        member __.markUsed tokenId =
            task {
                let! m = context.MagicLinks.FindAsync(tokenId)
                m.UsedOn <- Nullable(DateTime.UtcNow)
                context.MagicLinks.Update(m) |> ignore
                let! _ = context.SaveChangesAsync()
                return ()
            }
