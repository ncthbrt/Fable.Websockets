namespace Fable.Websockets

module Server =    
    open Protocol
    open System

    type OnConnectionEstablished<'serverProtocol, 'clientProtocol> = 
      CloseHandle -> IObservable<WebsocketEvent<'serverProtocol>> -> SendMessage<'clientProtocol> -> IDisposable
