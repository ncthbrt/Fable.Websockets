namespace Fable.Websockets

module Server =    
    open Protocol
    open Observables    
    open System        

    type OnConnectionEstablished<'serverProtocol, 'clientProtocol> = 
      CloseHandle -> IObservable<WebsocketEvent<'serverProtocol>> -> SendMessage<'clientProtocol> -> unit 
