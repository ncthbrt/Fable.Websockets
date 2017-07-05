// Learn more about F# at http://fsharp.org
module HelloWorld.Server

open System
open Fable.Websockets.Suave
open Suave
open Suave.Http
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Files
open Suave.RequestErrors
open Suave.Logging
open Suave.Utils
open HelloWorld.Protocol


type ServerState ={ currentDirectory:string; user: HelloWorld.Protocol.User option }

type Effect<'clientProtocol> = 
    
    | Send of 'clientProtocol
    | OpenDirectory of string 
    
let private onlySome obs = 
    obs 
    |> Observable.filter Option.isSome
    |> Observable.map Option.get
        
let inline reducer source (prev:ServerState, _) (msg:ServerMsg) =
    match msg with 
    | Greet user ->  ({prev with user = Some user; }, Some Welcome)
    | ListCurrentDirectory -> prev,None
    | ChangeDirectory dir ->  prev,None
    | GetFileContents file -> prev,None    

let onConnectionEstablished close messageObservable source = 
    let initialState = { currentDirectory="./wwwroot"; user = None }        
    do Challenge |> source     
    
    let reducer = messageObservable |> Observable.scan (fun (prev,action) msg -> (prev,action)) (initialState, None)    

    let effectObservable = 
        reducer 
        |> Observable.map snd 
        |> onlySome 
        
    effectObservable 
    |> Observable.subscribe source
 

let app : WebPart = 
  choose [
    path "/websocket" >=> websocket<ServerMsg,ClientMsg> onConnectionEstablished        
    NOT_FOUND "Found no handlers." 
  ]

[<EntryPoint>]
let main _ =
  startWebServer { defaultConfig with logger = Targets.create Verbose [||] } app
  0