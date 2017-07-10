// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.DotNetCli
open Fake.ProcessHelper
open Fake.FileSystem
open Fake.YarnHelper
open Fake.ReleaseNotesHelper


// Directories
let buildDir  = "./build/"
let deployDir = "./deploy/"



let projectFolders  =  [ (filesInDirMatchingRecursive "*.fsproj" (directoryInfo "./src"))  
                         (filesInDirMatchingRecursive "*.csproj" (directoryInfo "./src"))                         
                       ] 
                       |> Array.concat
                       |> Seq.map (fun m -> m.Directory.FullName)


Target "Clean" (fun _ ->
    CleanDirs [buildDir; deployDir]
)

Target "YarnRestore" (fun _->        
   ["./"; "./src/Fable.Websockets.Client/"]
   |> Seq.iter (fun dir -> Yarn (fun p ->{ p with Command = Install Standard; WorkingDirectory = dir}))
   |> ignore   
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

let release =  ReadFile "RELEASE_NOTES.md" |> ReleaseNotesHelper.parseReleaseNotes
                

Target "Meta" (fun _ ->
    [ "<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">"
      "<PropertyGroup>"
      "<Description>Library for strongly typed websocket use in Fable</Description>"
      "<PackageProjectUrl>https://github.com/ncthbrt/Fable.Websockets</PackageProjectUrl>"
      "<PackageLicenseUrl>https://github.com/ncthbrt/Fable.Websockets/blob/master/LICENSE.md</PackageLicenseUrl>"
      "<PackageIconUrl></PackageIconUrl>"
      "<RepositoryUrl>https://github.com/ncthbrt/Fable.Websockets</RepositoryUrl>"
      "<PackageTags>fable;fsharp;websockets;observables</PackageTags>"
      "<Authors>Nick Cuthbert</Authors>"
      sprintf "<Version>%s</Version>" (string release.SemVer)
      "</PropertyGroup>"
      "</Project>"]
    |> WriteToFile false "src/Meta.props"    
)

Target "Package" (fun _ ->        
    printfn "%A" currentDirectory
    projectFolders  
    |> Seq.iter (fun project-> DotNetCli.Pack (fun p-> { p with Project=project; OutputPath = currentDirectory+"/build" }))
)


// Build order
"Meta" 
    ==> "Clean" 
    ==> "Restore" 
    ==> "YarnRestore" 
    ==> "Build"
    ==> "Package"

// start build
RunTargetOrDefault "Build"
