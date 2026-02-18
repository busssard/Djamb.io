namespace Djambi.Api.Logic.Services

open System.Collections.Concurrent
open Serilog
open Djambi.Api.Common.Control
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Model
open Djambi.Api.Enums
open System.Threading.Tasks

type NotificationService(log : ILogger) =
    let subscribers = new ConcurrentDictionary<int, ISubscriber>()

    interface INotificationService with
        member x.add subscriber =
            log.Information(sprintf "Notifications: User %i subscribed to notifications using %s" subscriber.userId (subscriber.GetType().Name))
            subscribers.[subscriber.userId] <- subscriber

        member x.remove userId =
            log.Information(sprintf "Notifications: User %i unsubscribed from notifications" userId)
            match subscribers.TryGetValue userId with
            | (true, s) -> 
                s.Dispose()
                subscribers.TryRemove userId
                |> ignore
            | _ -> ()

        member x.send response =
            let creatorId = response.event.createdBy.userId

            // For turn events (TurnCommitted, TurnSkipped), actingPlayerId is None
            // when BotRunner acts because Neutral players have no userId. In that case
            // no human received the HTTP response, so notify all players.
            let notifyAllPlayers =
                response.event.actingPlayerId.IsNone
                && (response.event.kind = EventKind.TurnCommitted
                    || response.event.kind = EventKind.TurnSkipped)

            log.Information(
                "Notifications: Event {EventKind} for game {GameId}, creatorId={CreatorId}, actingPlayerId={ActingPlayerId}, notifyAll={NotifyAll}, subscribers=[{Subscribers}]",
                response.event.kind,
                response.game.id,
                creatorId,
                (response.event.actingPlayerId |> Option.map string |> Option.defaultValue "None"),
                notifyAllPlayers,
                (subscribers.Keys |> Seq.map string |> String.concat ", "))

            let otherPlayersUserIds =
                response.game.players
                |> Seq.choose (fun p ->
                    match p.userId with
                    | Some uId when notifyAllPlayers || uId <> creatorId -> Some uId
                    | _ -> None
                )

            let removedPlayersUserIds =
                response.event.effects
                |> Seq.choose (fun f ->
                    match f with
                    | Effect.PlayerRemoved f1 -> f1.oldPlayer.userId
                    | _ -> None
                )

            let userIds =
                otherPlayersUserIds
                |> Seq.append removedPlayersUserIds
                |> Seq.distinct
                |> Seq.toList

            log.Information(
                "Notifications: Will notify userIds=[{UserIds}]",
                (userIds |> List.map string |> String.concat ", "))

            subscribers.Values
            |> Seq.filter (fun s -> userIds |> List.contains s.userId)
            |> Seq.map (fun s ->
                task {
                    try
                        return! s.send response
                    with
                    | :? DjambiWebsocketException ->
                        (x :> INotificationService).remove s.userId
                        return ()
                } :> Task
            )
            |> Task.WhenAll
            |> Task.toGeneric