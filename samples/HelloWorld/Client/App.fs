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
    | NoViewModel  

type ConnectionState = NotConnected | Connected    

type Model = { viewModel: ViewModel; 
               connectionState: ConnectionState; 
               socket: SocketHandle<ServerMsg,ClientMsg>
               email: string
               folder: (string*(FileReference list)) option
             }

type ApplicationMsg = 
    | SetEmailText of string
    | SubmitUserEmail of string    
    | OpenChildFolder of string
    | OpenFile of string

type MsgType = Msg<ServerMsg,ClientMsg,ApplicationMsg>

let inline initialState () =  
    ({ connectionState = NotConnected
       viewModel = EmailPrompt ""
       email="" 
       socket = SocketHandle.Blackhole() 
       folder = None 
     }, Cmd.none)

let inline socketMsgUpdate (msg:ClientMsg) prevState = 
    match msg with    
    | ClientMsg.Challenge -> prevState, Cmd.ofSocketMessage prevState.socket (Greet {email=prevState.email})
    | Welcome -> prevState, Cmd.ofSocketMessage prevState.socket ListCurrentDirectory
    | DirectoryListing d -> { prevState with folder = Some d } , Cmd.none
    | NotFound fileRef -> prevState, Cmd.none
    | DirectoryChanged fileRef -> prevState, Cmd.ofSocketMessage prevState.socket ListCurrentDirectory
    | FileContents contents -> prevState, Cmd.none    
let inline applicationMsgUpdate (msg: ApplicationMsg) prevState =
    match msg with
    | SubmitUserEmail email -> ({ prevState with viewModel = NoViewModel }, Cmd.tryOpenSocket "ws://localhost:8083/websocket")
    | SetEmailText email -> ({ prevState with viewModel = EmailPrompt email; email = email }, Cmd.none)
    | OpenChildFolder folder -> prevState, Cmd.ofSocketMessage prevState.socket (MoveToSubdirectory folder)
    | OpenFile file -> prevState, Cmd.ofSocketMessage prevState.socket (GetFileContents file)    

let inline update msg prevState = 
    match msg with
    | ApplicationMsg amsg -> applicationMsgUpdate amsg prevState
    | WebsocketMsg (socket, Opened) -> ({ prevState with socket = socket; connectionState = Connected }, Cmd.none)    
    | WebsocketMsg (_, Msg socketMsg) -> (socketMsgUpdate socketMsg prevState)
    | _ -> (prevState, Cmd.none)

let emailView (email:string) dispatch =        
    R.div[] [
        R.h1 [] [R.str "Enter your email"]
        R.br []
        R.input [Value (!^email); OnChange (fun e-> (dispatch<<ApplicationMsg<<SetEmailText<<string) e.target?value)]
        R.input [Type "submit"; OnClick (fun e-> (dispatch<<ApplicationMsg<<SubmitUserEmail<<string) email)]
    ]

let fileEntryView dispatch fileReference =
    match fileReference with
    | File file -> [R.a [Href "#";OnClick (fun _ -> (dispatch<<ApplicationMsg<<OpenFile) file)] [R.str ("📝" + file)]; R.br []]
    | Folder folder -> [R.a [Href "#"; OnClick (fun _ -> (dispatch<<ApplicationMsg<<OpenChildFolder) folder)] [R.str ("📁" + folder)]; R.br []]    

let folderView (folder: string, files: FileReference list) dispatch =        
    let headers = [R.h1 [] [R.str <| "☝️ "+folder];R.br []] 
    let files = files 
                |> List.fold (fun prev fileReference -> (fileEntryView dispatch fileReference) @ prev) [] 
                |> List.rev

    R.div [] (headers @ files)
        

let view model dispatch = 
    let loader = R.text [] [R.str "loading..."]    

    match model.viewModel with
    | EmailPrompt email -> emailView email dispatch 
    | NoViewModel -> 
        match model.connectionState with
        | NotConnected -> loader
        | Connected -> match model.folder with
                       | None ->  loader
                       | Some f -> folderView f dispatch
    

Program.mkProgram initialState update view
|> Program.withReact "root"
|> Program.withConsoleTrace
|> Program.run
