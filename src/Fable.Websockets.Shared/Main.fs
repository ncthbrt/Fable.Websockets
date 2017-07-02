namespace Fable.Websockets

module Protocols =

    type ClientMsg<'client> = 
        | Message of 'client
        | Connected 
        | Disconnected
        | Error

    type ServerMsg<'server> = 
        | Message of 'server
        | Connected 
        | Disconnected
        | Error of string




    
