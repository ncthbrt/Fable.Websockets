namespace Fable.Websockets.Elmish

open System
open Fable.Core
open Fable.Websockets.Protocol
open FSharp.Collections
open Fable.Websockets.Client
open Fable.Websockets.Protocol
// type Program<'arg, 'model, 'msg, 'view> = {
//     init : 'arg -> 'model * Cmd<'msg>
//     update : 'msg -> 'model -> 'model * Cmd<'msg>
//     subscribe : 'model -> Cmd<'msg>
//     view : 'model -> Dispatch<'msg> -> 'view
//     setState : 'model -> Dispatch<'msg> -> unit
//     onError : (string*exn) -> unit
// }
type ConnectionState = NotConnected | Connected

type WebsocketModel<'model, 'serverMsg, 'clientMsg> = 
    { applicationModel: 'model; 
      websocketSubscription: IDisposable option; 
      websocketSink: ('serverMsg -> unit); 
      connectionState: ConnectionState 
    }


type Msg<'clientMsg, 'applicationMsg> =
    | Websocket of WebsocketEvent<'clientMsg>
    | Application of 'applicationMsg

type WebsocketConnection =         
    private { closeHandle: ClosedCode -> string -> unit; sink: obj->unit; subscription: IDisposable }
    
    static member private UntypedSink<'serverMsg> (sink:'serverMsg->unit) (msg:obj) =
        match msg with
        | :? 'serverMsg as m -> sink m
        | _ -> failwith "Fail this message is of the incorrect type"

    static member Create<'serverMsg,'clientMsg> address dispatcher= 
        let (sink,source, closeHandle) = establishWebsocketConnection<'serverMsg,'clientMsg> address            

        let websocketSubscription = source |> Observable.subscribe (Websocket >> dispatcher)
        let websocketSink = (WebsocketConnection.UntypedSink<'serverMsg> sink)
        let websocketCloseHandle = closeHandle
        
        { closeHandle=closeHandle; sink= websocketSink; subscription=websocketSubscription }
        

module Cmd =
    open System
    open FSharp.Collections

    open Fable.Websockets.Client
    open Fable.Websockets.Protocol

    let mutable private connections : Map<string,WebsocketConnection> = Map.empty    
    
    let public ofSocketMessage (message:'serverMsg): Elmish.Cmd<'msg> =         
        [fun dispatch -> websocketSink message]
    

    let public openSocket<'serverMsg,'clientMsg> address =         
        match websocketSubscription with
        | Some _ -> failwith "This library only supports a single websocket connection at a time"
        | None -> 
            [fun dispatcher ->
                let (sink,source, closeHandle) = establishWebsocketConnection<'serverMsg,'clientMsg> address            

                websocketSubscription <- source |> Observable.subscribe (Websocket >> dispatcher) |> Some                                                
                websocketSink  <- (untypedSink<'serverMsg> sink)
                websocketCloseHandle <- closeHandle
            ]

    let public closeSocket code reason =         
        [ fun _ -> websocketCloseHandle code reason ]


