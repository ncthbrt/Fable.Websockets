namespace Fable.Websockets

module Suave =
    open Thoth.Json.Net
    open Microsoft.FSharp.Control
    open Fable.Websockets.Protocol    
    open Fable.Websockets.Server
    open Fable.Websockets.Observables
    open Suave.Sockets
    open Suave.Sockets.Control
    open Suave.WebSocket
    open Suave.Http    
    open System.Threading

    module internal UTF8 =
      let bytes(s : string) = System.Text.Encoding.UTF8.GetBytes s
      let toString(bytes : byte[]) = System.Text.Encoding.UTF8.GetString bytes

    let inline toJson<'T> (x: 'T) = Encode.Auto.toString(0, x)
    let inline fromJson<'T> json = Decode.Auto.unsafeFromString<'T>(json)

    let private flip f a b = f (b,a)

    let private sendCloseFrame (webSocket:WebSocket) (code:ClosedCode) reason = socket {        
        let firstBytes = code |> fromClosedCode |> System.BitConverter.GetBytes |> Array.rev  // First 16 bits are for code
        let secondBytes = reason |> UTF8.bytes // Rest is for reason
        let payload = Array.concat [firstBytes;secondBytes] |> ByteSegment                       
        do! webSocket.send Close payload true              
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
          let code = data |> Array.take 2 |> Array.rev |> (flip System.BitConverter.ToUInt16) 0 |> toClosedCode
          let reason = data |> Array.skip 2 |> UTF8.toString                                      
          WebsocketEvent.Closed {code=code; reason=reason; wasClean=true}

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
                
        let receiveSubject = Subject<WebsocketEvent<'serverProtocol>> ()
        let sendSubject = Subject<'clientProtocol> ()

        let closeHandle code reason =
          if cancellationTokenSource.IsCancellationRequested then
            ()
          else 
            cancellationTokenSource.Cancel()
            sendSubject.Completed()
            receiveSubject.Completed()

            sendCloseFrame websocket code reason 
            |> Async.RunSynchronously
            |> ignore 

        // Hold reference to subscription until we exit this connection scope    
        use applicationSubscription = onConnection closeHandle receiveSubject sendSubject.Next        
        
        // Subscribe to messages from server to client.
        // Forward them to client
        use subscription = sendSubject.Subscribe (sendMessage websocket)                        
        
        // Send the subject a message indicating that the connection has been opened
        do receiveSubject.Next Opened

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
              do receiveSubject.Next msg
                    
      }

    let public websocket<'serverProtocol,'clientProtocol> (onConnectionEstablished:OnConnectionEstablished<'serverProtocol, 'clientProtocol>) =
      handShake (ws onConnectionEstablished)

