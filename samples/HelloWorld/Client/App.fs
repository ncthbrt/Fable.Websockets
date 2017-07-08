module HelloWorld.Client

open HelloWorld.Protocol


open Fable.Core
open Fable.Core.JsInterop
open Fable.Import

open Elmish
open Elmish.React

open Fable.Websockets.Client


open Fable.Helpers.React.Props
module R = Fable.Helpers.React

type ConnectionState = NotConnected | Connected

type ViewModel = 
    | FileView of FileContents
    | FolderListing of string*(FileReference list)    

type Model = { connectionState: ConnectionState; viewModel: ViewModel option }


type ClientEvent = | WebsocketEvent of Fable.Websockets.Protocol.WebsocketEvent<ClientMsg>                       
let initialState () = 
    { connectionState = NotConnected; viewModel = None }, Cmd.none

let reducer event prevState = 
    match event with 
    | WebsocketEvent e -> 
        prevState, Cmd.none    


let mutable observableSubscription:System.IDisposable Option = None

let websocketSubscription initialState =
    let subscription dispatcher = 
        let (sink,source, closeHandle) = establishWebsocketConnection<ServerMsg,ClientMsg> "ws://127.0.0.1:8083/websocket"
        
        let subscription = source |> Observable.subscribe (ClientEvent.WebsocketEvent >> dispatcher)        
        observableSubscription <- Some subscription
        ()
                    
    Cmd.ofSub subscription


let view model dispatcher = 
    R.div [] [R.str "This is a test message"]

Program.mkProgram initialState reducer view
|> Program.withReact "root"
|> Program.withSubscription websocketSubscription
|> Program.run