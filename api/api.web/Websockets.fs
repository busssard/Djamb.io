namespace Djambi.Api.Web.Websockets

open System
open System.Net.WebSockets
open System.Text
open System.Threading
open Newtonsoft.Json
open Newtonsoft.Json.Converters
open Newtonsoft.Json.Serialization
open Serilog
open Djambi.Api.Common.Control
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Model
open Djambi.Api.Web.Mappings

type WebsocketSubscriber(userId : int,
                         socket : WebSocket,
                         log : ILogger) =
    static let serializerSettings =
        let s = JsonSerializerSettings()
        s.ContractResolver <- CamelCasePropertyNamesContractResolver()
        s.Converters.Add(StringEnumConverter())
        s

    let serializeResponse (response : StateAndEventResponse) =
        let dto = response |> toStateAndEventResponseDto
        JsonConvert.SerializeObject(dto, serializerSettings)

    let writeMessage (json : string) =
        let buffer = Encoding.UTF8.GetBytes(json)
        let segment = new ArraySegment<byte>(buffer)

        if socket.State = WebSocketState.Open then            
            try 
                socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None)
                |> Task.toGeneric
            with 
            | _ -> raise <| DjambiWebsocketException("Socket error")
        else raise <| DjambiWebsocketException("Socket closed")
    
    interface ISubscriber with
        member x.userId = userId
        member x.send response =
            log.Information(sprintf "WS: Sending event to User %i" userId)
            response |> serializeResponse |> writeMessage
        member x.Dispose() =
            socket.Dispose()