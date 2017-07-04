namespace Fable.Websockets

module Suave =
    open Newtonsoft.Json
    open Microsoft.FSharp.Control
    open Fable.Websockets.Protocol    
    open Fable.Websockets.Server
    open Fable.Websockets.Observables
    open Suave.Sockets
    open Suave.Sockets.Control
    open Suave.WebSocket
    open Suave.Http    
    open System.Threading

    let private jsonConverter = Fable.JsonConverter() :> JsonConverter
    let private fromJson value = JsonConvert.DeserializeObject(value, [|jsonConverter|])
    let private toJson value = JsonConvert.SerializeObject(value, [|jsonConverter|])
    let private flip f a b = f (b,a)

    let private sendCloseFrame (webSocket:WebSocket) (code:ClosedCode) reason = socket {        
        let firstBytes = code |> fromClosedCode |> System.BitConverter.GetBytes // First 16 bits are for code
        let secondBytes = reason |> UTF8.bytes // Rest is for reason
        let okStatusCode = Array.concat [firstBytes;secondBytes] |> ByteSegment              
        do! webSocket.send Close okStatusCode true              
    }

    let private sendMessage<'clientProtocol> (webSocket: WebSocket) (payload:'clientProtocol) = 
      (async {
        let! socketResult = socket {
            let jsonVal = toJson payload |> UTF8.bytes |> ByteSegment
            do! webSocket.send Text jsonVal true                        
        }
        return ()
      }) |> Async.StartImmediate

    let private decodeMsgPayload<'serverProtocol> data: WebsocketEvent<'serverProtocol> = 
       try  
          (UTF8.toString >> fromJson >> Msg) data
       with 
          | e -> Exception e       

    let private decodeClosedPayload<'serverProtocol> data: WebsocketEvent<'serverProtocol> = 
          let code = data |> Array.take 2 |> (flip System.BitConverter.ToUInt16) 0 |> toClosedCode
          let reason = data |> Array.skip 2 |> UTF8.toString                                      
          Closed {code=code; reason=reason; wasClean=true}

    let inline private readMessage<'serverProtocol> data: WebsocketEvent<'serverProtocol> option = 
      match data with
      | (Text, data, true) ->  decodeMsgPayload<'serverProtocol> data |> Some
      | (Close, data, _) -> decodeClosedPayload<'serverProtocol> data |> Some
      | _ -> None

    let private ws<'serverProtocol,'clientProtocol> (onConnection:OnConnectionEstablished<'serverProtocol, 'clientProtocol>) 
                                                    (websocket : WebSocket) 
                                                    (_: HttpContext) =      
      socket {

        use cancellationTokenSource = new CancellationTokenSource()
        let token = cancellationTokenSource.Token      
                
        let closeHandle code reason =
          if cancellationTokenSource.IsCancellationRequested then
            ()
          else 
            do cancellationTokenSource.Cancel()

            sendCloseFrame websocket code reason 
            |> Async.RunSynchronously
            |> ignore                                         

        let subject = Subject<WebsocketEvent<'serverProtocol>> ()

        let onClientMessageObservable = onConnection closeHandle subject
        
        // Subscribe to messages from server to client.
        // Forward them to client
        let subscription = onClientMessageObservable.Subscribe (sendMessage websocket)
        
        // Send the subject a message indicating that the connection has been opened
        do subject.Next Opened

        while not token.IsCancellationRequested do
          // Get next message
          let! rawMsg = websocket.read()
          let msg = rawMsg |> readMessage<'serverProtocol>
          
          match msg with           
          | None -> () // Message is ignored in application level protocol
          | Some msg ->                                       
              match msg with 
              | WebsocketEvent.Closed  { code=code ; reason=reason; wasClean=_ } -> 
                // Cancel iteration and send client the close frame
                closeHandle code reason
              | _ -> ()            
              
              // Send the server observable the current message
              do subject.Next msg
      }

    let public websocket<'serverProtocol,'clientProtocol> (onConnectionEstablished:OnConnectionEstablished<'serverProtocol, 'clientProtocol>) =
      handShake (ws onConnectionEstablished)

