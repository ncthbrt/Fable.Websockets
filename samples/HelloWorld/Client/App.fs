module HelloWorld.Client

open System

open HelloWorld.Protocol

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import

open Elmish
open Elmish.React

open Fable.Websockets.Client
open Fable.Websockets.Protocol

open Fable.Helpers.React.Props
module R = Fable.Helpers.React

open Fable.Websockets.Elmish
open Fable.Websockets.Elmish.Types

type ViewModel = 
    | EmailPrompt of string
    | Loading
    | FileView of FileContents    
    | FolderListing of string*(FileReference list)    

type ConnectionState = NotConnected | Connected    

type Model = { viewModel: ViewModel; 
               connectionState: ConnectionState; 
               socket: SocketHandle<ServerMsg,ClientMsg>
               email: string
             }

type ApplicationMsg = 
    | SetEmailText of string
    | SubmitUserEmail of string

type MsgType = Msg<ServerMsg,ClientMsg,ApplicationMsg>

let inline initialState () =  
    ({ connectionState = NotConnected; viewModel = EmailPrompt ""; email=""; socket = SocketHandle.Blackhole() }, Cmd.none)

let inline socketMsgUpdate (msg:ClientMsg) prevState = 
    match msg with    
    | ClientMsg.Challenge -> prevState, Cmd.ofSocketMessage prevState.socket (Greet {email=prevState.email})
    | Welcome -> prevState, Cmd.none
    | DirectoryListing files -> prevState, Cmd.none
    | NotFound fileRef -> prevState, Cmd.none
    | DirectoryChanged fileRef -> prevState, Cmd.none
    | FileContents contents -> prevState, Cmd.none    


let inline update msg prevState = 
    match msg with
    | ApplicationMsg (SubmitUserEmail email) -> ({ prevState with viewModel = Loading }, Cmd.tryOpenSocket "ws://localhost:8083/websocket")
    | ApplicationMsg (SetEmailText email) -> ({ prevState with viewModel = EmailPrompt email; email = email }, Cmd.none)
    | WebsocketMsg (socket, Opened) -> ({ prevState with socket = socket; connectionState = Connected }, Cmd.none)    
    | WebsocketMsg (socket, Msg socketMsg) -> (socketMsgUpdate socketMsg prevState)            
    | _ -> (prevState, Cmd.none)


let emailView (email:string) dispatch =        
    R.div[] [
        R.h1 [] [R.str "Enter your email"]
        R.br []
        R.input [Value (!^email); OnChange (fun e-> (dispatch<<ApplicationMsg<<SetEmailText<<string) e.target?value)]
        R.input [Type "submit"; OnClick (fun e-> (dispatch<<ApplicationMsg<<SubmitUserEmail<<string) email)]
    ]

let view model dispatch = 
    match model.viewModel with
    | EmailPrompt email -> emailView email dispatch 
    | Loading -> R.text [] [R.str "loading..."]
    | _ -> R.div [] [R.str "This is a test message"] 

Program.mkProgram initialState update view
|> Program.withReact "root"
|> Program.withConsoleTrace
|> Program.run
