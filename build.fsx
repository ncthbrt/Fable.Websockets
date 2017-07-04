// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.DotNetCli
open Fake.FileSystem

// Directories
let buildDir  = "./build/"
let deployDir = "./deploy/"


// Filesets
let appReferences  =
    !! "/**/*.csproj"
    ++ "/**/*.fsproj"    


let projectFolders  =  [ (filesInDirMatchingRecursive "*.fsproj" (directoryInfo "./"));  
                         (filesInDirMatchingRecursive "*.csproj" (directoryInfo "./"))
                       ] 
                       |> Array.concat
                       |> Seq.map (fun m -> m.Directory.FullName)

// version info
let version = "0.1"  // or retrieve from CI server

// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir; deployDir]
)

Target "Restore" (fun _->    
    projectFolders
    |> Seq.map (fun project-> DotNetCli.Restore (fun p-> {p with Project=project}))
    |> Seq.toArray    
    |> ignore
)

Target "Build" (fun _ ->
    projectFolders
    |> Seq.map (fun project-> DotNetCli.Build (fun p-> { p with Project=project }))
    |> Seq.toArray    
    |> ignore        
)



// Build order
"Clean" ==> "Restore" ==> "Build"  

// start build
RunTargetOrDefault "Build"
