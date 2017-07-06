module HelloWorld.Client

open HelloWorld.Protocol


open Fable.Core
open Fable.Core.JsInterop
open Fable.Import

open Elmish

open Fable.Websockets.Client
open Fable.Websockets.Observables

type ConnectionState = NotConnected | Connected

type ViewModel = 
    | FileView of FileContents
    | FolderListing of string*(FileReference list)    

type Model = { connectionState: ConnectionState; viewModel: ViewModel option }

let initialState () = { connectionState = NotConnected; viewModel = None }

let reducer prevState event = 
    prevState, Cmd.none

let websocketSubscription initialState =
    let subscription dispatcher = 
        let (source, sink, closeHandle) = establishWebsocketConnection "ws://localhost:8083/"
        ()
                
    Cmd.ofSub subscription

let init() =
    let canvas = Browser.document.getElementsByTagName_canvas().[0]
    canvas.width <- 1000.
    canvas.height <- 800.
    let ctx = canvas.getContext_2d()
    // The (!^) operator checks and casts a value to an Erased Union type
    // See http://fable.io/docs/interacting.html#Erase-attribute
    ctx.fillStyle <- !^"rgb(200,0,0)"
    ctx.fillRect (10., 10., 55., 50.)
    ctx.fillStyle <- !^"rgba(0, 0, 200, 0.5)"
    ctx.fillRect (30., 30., 55., 50.)

init()