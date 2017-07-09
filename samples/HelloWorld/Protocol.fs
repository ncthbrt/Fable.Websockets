module HelloWorld.Protocol

type User = { email:string; }

type FileReference = 
    | File of string 
    | Folder of string

type FileContents = { name:string; contents: byte[] }

type ClientMsg =
    | Challenge // Server asks client to identify itself    
    | Welcome of string // Challenge was received. User can now perform queries  
    | DirectoryListing of FileReference list  // Response to ListCurrentDirectory
    | NotFound of FileReference // Response to OpenDirectory or GetFileContents when path doesn't exist
    | DirectoryChanged of FileReference list // Acknowledgement that directory has changed
    | FileContents of FileContents // Response to GetFileContents


type ServerMsg =        
    | Greet of User  // Client greets the server after challenge
    | ListCurrentDirectory // Asks server to send list of current files and folders in directory 
    | MoveToSubdirectory of string // Asks server to navigate to directory    
    | MoveToParentDirectory
    | GetFileContents of string // Asks server to open file and return file contents


