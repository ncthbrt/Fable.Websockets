namespace Fable.Websockets

module Server =    
    open Protocol
    open Observables
    open Microsoft.FSharp.Control
    open System    

    type OnConnectionEstablished<'serverProtocol, 'clientProtocol> = 
      CloseHandle -> IObservable<WebsocketEvent<'serverProtocol>> -> IObservable<'clientProtocol>
