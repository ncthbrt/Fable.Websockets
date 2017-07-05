module Fable.Websockets.Client

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser

open Fable.Websockets.Observables
open Fable.Websockets.Protocol

let private toObj () = obj()
let private receiveMessage<'clientProtocol> (receiveSubject:Subject<WebsocketEvent<'clientProtocol>>) (msgEvent:MessageEvent) =         
    try
        (Msg << ofJson << string) msgEvent.data             
    with     
        | e -> Exception e   
    |> receiveSubject.Next
    |> toObj

let private receiveCloseEvent<'clientProtocol, 'serverProtocol> (receiveSubject:Subject<WebsocketEvent<'clientProtocol>>) (sendSubject:Subject<'serverProtocol>) (closeEvent:CloseEvent) =         
    do sendSubject.Completed()
    do receiveSubject.Completed()
    let closedCode = (toClosedCode<<uint16) closeEvent.code
    let payload  = { code = closedCode; reason= closeEvent.reason; wasClean=closeEvent.wasClean }    
    
    payload 
    |> WebsocketEvent.Closed 
    |> receiveSubject.Next  
    |> toObj

let private sendMessage (websocket:WebSocket) (receiveSubject:Subject<WebsocketEvent<'a>>) msg =    
    try 
        let jsonMsg = msg |> toJson
        do websocket.send jsonMsg    
    with 
        | e -> receiveSubject.Next (Exception e)
    


let public establishConnection<'serverProtocol, 'clientProtocol> (uri:Uri) = 

    let receiveSubject = Observables.Subject<WebsocketEvent<'clientProtocol>>()
    let sendSubject = Observables.Subject<'serverProtocol>()
    
    let websocket = WebSocket.Create(uri.AbsolutePath)
    
    let connection = (sendSubject.Subscribe (sendMessage websocket receiveSubject))

    let closeHandle (code:ClosedCode) (reason:string) = 
        let state = websocket.readyState |> uint16 |> toReadyState
        if state=Connecting || state=Open then                
            websocket.close((float<<fromClosedCode) code, reason)          
            connection.Dispose() 
        else ()    

    websocket.onmessage <- fun msg -> (receiveMessage<'clientProtocol> receiveSubject msg)
    websocket.onclose <- fun msg -> receiveCloseEvent receiveSubject sendSubject msg
    websocket.onopen <- fun _ -> receiveSubject.Next Opened |> toObj                                 
    websocket.onerror <- fun _ -> receiveSubject.Next Error |> toObj                                     
    
    (receiveSubject, sendSubject.Next, closeHandle)