namespace Djambi.Api.Web.Controllers

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Primitives

open Djambi.Api.Common.Control
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Web.Authentication
open Djambi.Api.Web.Sse
open Djambi.Api.Web.Websockets

[<ApiController>]
[<Authorize>]
[<Route("api/notifications")>]
type NotificationController(service : INotificationService) =
    inherit ControllerBase()

    let contentType = "text/event-stream"
    let checkForCloseDelay = TimeSpan.FromSeconds(3.0)

    [<HttpGet("sse")>]
    [<ProducesResponseType(200)>]
    member __.ConnectSse() : Task =
        let ctx = base.HttpContext

        if (ctx.Request.Headers.["Accept"] <> StringValues(contentType))
        then raise <| InvalidWebRequestException(sprintf "Accept header must be '%s'." contentType)

        task {
            let session = ctx.GetSession()

            ctx.Response.Headers.["Content-Type"] <- StringValues(contentType)
            ctx.Response.Body.Flush()

            let userId = session.user.id
            let subscriber = new SseSubscriber(userId, ctx.Response, Serilog.Log.Logger)

            service.add subscriber

            while not ctx.RequestAborted.IsCancellationRequested do
                do! Task.Delay checkForCloseDelay
            service.remove userId
        }

    [<HttpGet("ws")>]
    [<ProducesResponseType(200)>]
    member __.ConnectWebSockets() : Task =
        let ctx = base.HttpContext

        if not ctx.WebSockets.IsWebSocketRequest
        then raise <| InvalidWebRequestException("This endpoint requires a websocket request.")

        task {
            let session = ctx.GetSession()

            let! socket = ctx.WebSockets.AcceptWebSocketAsync()

            let userId = session.user.id
            let subscriber = new WebsocketSubscriber(userId, socket, Serilog.Log.Logger)
            service.add subscriber

            // Keep connection alive by reading until client disconnects
            let buffer : byte[] = Array.zeroCreate 4096
            let mutable keepAlive = true
            while keepAlive do
                let! result = socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)
                if result.MessageType = Net.WebSockets.WebSocketMessageType.Close then
                    keepAlive <- false

            service.remove userId
            if socket.State = Net.WebSockets.WebSocketState.Open
               || socket.State = Net.WebSockets.WebSocketState.CloseReceived then
                do! socket.CloseAsync(
                    Net.WebSockets.WebSocketCloseStatus.NormalClosure, "", CancellationToken.None)
        }
