module HelloWorld.Client

open System

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


type WebsocketModel<'a> = 
    { applicationModel: 'a; 
      websocketSubscription: IDisposable option; 
      websocketSink: (ServerMsg -> unit); 
      connectionState: ConnectionState 
    }
        
let initialWebsocketState initFunction =
    let (appModel,cmd) = initFunction()
    ({
        applicationModel = appModel
        websocketSubscription = None
        websocketSink = ignore
        connectionState = NotConnected
    }, cmd)

type ApplicationModel = { viewModel: ViewModel  Option}



type ClientEvent = | WebsocketEvent of Fable.Websockets.Protocol.WebsocketEvent<ClientMsg>              
                   | SubscriptionCreated of IDisposable*(ServerMsg -> unit)

let initialState () = ({ connectionState = NotConnected; 
                         viewModel = None; 
                         websocketSubscription = None; 
                         websocketSink = ignore 
                       }, Cmd.none
                      )


let reducer event prevState = 
    match event with 
    | WebsocketEvent e -> prevState, Cmd.none    
    | SubscriptionCreated (subscription,sink) -> {prevState with websocketSubscription = Some subscription; }, Cmd.none


let mutable observableSubscription:System.IDisposable Option = None

let websocketSubscription initialState =
    let subscription dispatcher = 
        let (sink,source, closeHandle) = establishWebsocketConnection<ServerMsg,ClientMsg> "ws://127.0.0.1:8083/websocket"

        source 
        |> Observable.subscribe (ClientEvent.WebsocketEvent >> dispatcher)        
        |> SubscriptionCreated sink
        |> dispatcher        

                    
    Cmd.ofSub subscription


let view model dispatcher = 
    R.div [] [R.str "This is a test message"]

Program.mkProgram initialState reducer view
|> Program.withReact "root"

|> Program.withSubscription websocketSubscription
|> Program.run