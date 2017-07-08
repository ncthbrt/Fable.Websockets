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

let initialState () = 
    { connectionState = NotConnected; viewModel = None }, Cmd.none

let reducer prevState event = 
    prevState, Cmd.none

let websocketSubscription initialState =
    let subscription dispatcher = 
        let (sink,source, closeHandle) = establishWebsocketConnection<ServerMsg,ClientMsg> "ws://localhost:8083/"
        source
        |> Observable.subscribe (fun x->)

        ()
                    
    Cmd.ofSub subscription


let view model dispatcher = 
    R.div [] [R.str "This is a test message"]

Program.mkProgram initialState reducer view
|> Program.withReact "root"
|> Program.withSubscription websocketSubscription
|> Program.run