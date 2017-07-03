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

    let private ws<'serverProtocol,'clientProtocol> (webSocket : WebSocket) (context: HttpContext) handler =
        socket {
        // if `loop` is set to false, the server will stop receiving messages
        let mutable loop = true

        while loop do
          // the server will wait for a message to be received without blocking the thread
          let! msg = webSocket.read()

          match msg with
          // the message has type (Opcode * byte [] * bool)
          //
          // Opcode type:
          //   type Opcode = Continuation | Text | Binary | Reserved | Close | Ping | Pong
          //
          // byte [] contains the actual message
          //
          // the last element is the FIN byte, explained later
          | (Text, data, true) ->
                        
            let str = UTF8.toString data
            
            let socketEvent =
              try
                let command:'serverProtocol = fromJson str
                Msg command
              with 
                | :? Newtonsoft.Json.JsonException as e -> Error <| Some e.ToString()
                          
            
            // the response needs to be converted to a ByteSegment
            let byteResponse =
              response
              |> System.Text.Encoding.UTF8.GetBytes
              |> ByteSegment
            
            // the `send` function sends a message back to the client
            do! webSocket.send Text byteResponse true

          | (Close, _, _) ->
            let emptyResponse = [||] |> ByteSegment
            do! webSocket.send Close emptyResponse true

            // after sending a Close message, stop the loop
            loop <- false

          | _ -> ()
        }


    // let public create<'serverProtocol,'clientProtocol> handler = 
    //     handShake (ws<'serverProtocol,'clientProtocol>)