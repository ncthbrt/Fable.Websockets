namespace Fable.Websockets

module Suave =
    open Newtonsoft.Json
    open Fable.Websockets.Protocol
    open Suave.Sockets
    open Suave.Sockets.Control
    open Suave.WebSocket
    open Suave.Http
    

    let private jsonConverter = Fable.JsonConverter() :> JsonConverter
    let private fromJson value = JsonConvert.DeserializeObject(value, [|jsonConverter|])
    let private toJson value = JsonConvert.SerializeObject(value, [|jsonConverter|])
    let private flip f a b = f (b,a)

    let sendCloseFrame (webSocket:WebSocket) (code:ClosedCode) reason = socket {        
        let firstBytes = code |> fromClosedCode |> System.BitConverter.GetBytes // First 16 bits are for code
        let secondBytes = reason |> UTF8.bytes // Rest is for reason
        let okStatusCode = Array.concat [firstBytes;secondBytes] |> ByteSegment              
        do! webSocket.send Close okStatusCode true              
    }

    
    let private sendMessage<'clientProtocol> (webSocket: WebSocket) (payload:'clientProtocol) = async {
      let! socketResult = socket {
          let jsonVal = toJson payload |> UTF8.bytes |> ByteSegment
          do! webSocket.send Text jsonVal true                        
      }
      return ()
    }

    let toSocketOp (task : Async<unit>): Async<Choice<unit,Error>> = 
      async {
        let! _ = task
        return (Choice1Of2 ())
      }

    // TODO: implement proper error handling semantics
    let fromSocketOp (task : SocketOp<unit>): Async<unit> = 
      async {
        let! _ = task
        return ()
      }


    let private ws<'serverProtocol,'clientProtocol> (webSocket : WebSocket) (context: HttpContext) onConnection =      
      socket { 
        // if `loop` is set to false, the server will stop receiving messages        
        let mutable loop = true        
        
        let onMessageObservable : (SocketEvent<'serverProtocol> -> Async<unit>) = 
            onConnection context 
                         (sendMessage<'clientProtocol> webSocket)                          
                         (fun code reason -> 
                            (socket {                      
                                loop <-false
                                let! result = (sendCloseFrame webSocket code reason)
                                return ()
                              } |> fromSocketOp)
                         )

        let onMessage = onMessageObservable >> toSocketOp

        do! onMessage Opened

        while loop do
          // the server will wait for a message to be received without blocking the thread
          let! msg = webSocket.read()
          
          do! socket {
            match msg with          
            | (Text, data, true) ->
                          
              let str = UTF8.toString data
                          
              let msg=
                try
                  let command:'serverProtocol = fromJson str
                  Msg command
                with 
                  | :? Newtonsoft.Json.JsonException as e -> SocketEvent.Error << Some <| e.ToString ()
                 
              do! onMessage msg 

            | (Close, data, _) ->              
              let code = data |> Array.take 2 |> (flip System.BitConverter.ToUInt16) 0 |> toClosedCode
              let reason = data |> Array.skip 2 |> UTF8.toString                                                        
              
              // Respond to close request appropriately. End loop             
              do! sendCloseFrame webSocket Normal "None"              
              loop <- false
              let event = SocketEvent.Closed {code=code; reason=reason; wasClean=true}
              do! onMessage event

            | _ -> return ()
          }                 
      }