#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.JavaScript

open Fake.Core.TargetOperators

// Directories
let buildDir  = "./build/"
let deployDir = "./deploy/"



let projectFolders  =  [ (DirectoryInfo.getMatchingFilesRecursive "*.fsproj" (DirectoryInfo.ofPath "./src"))  
                         (DirectoryInfo.getMatchingFilesRecursive "*.csproj" (DirectoryInfo.ofPath "./src"))                         
                       ] 
                       |> Array.concat
                       |> Seq.map (fun m -> m.Directory.FullName)


Target.create "Clean" (fun _ ->
    Shell.cleanDirs [buildDir; deployDir]
)

Target.create "YarnRestore" (fun _->        
   ["./"; "./src/Fable.Websockets.Client/"]
   |> Seq.iter (fun dir -> Yarn.install (fun p -> { p with WorkingDirectory = dir}))
   |> ignore   
)

Target.create "Restore" (fun _->    
    projectFolders
    |> Seq.map (DotNet.restore id)
    |> Seq.toArray    
    |> ignore
)

Target.create "Build" (fun _ ->
    projectFolders
    |> Seq.map (DotNet.build id)
    |> Seq.toArray    
    |> ignore        
)

let release =  File.read "RELEASE_NOTES.md" |> ReleaseNotes.parse

Target.create "Meta" (fun _ ->
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
    |> File.write false "src/Meta.props"    
)

Target.create "Package" (fun _ ->        
    printfn "%A" System.Environment.CurrentDirectory
    projectFolders  
    |> Seq.iter (DotNet.pack (fun p-> { p with OutputPath = Some <| System.Environment.CurrentDirectory + "/build" }))
)


// Build order
"Meta" 
    ==> "Clean" 
    ==> "Restore" 
    ==> "YarnRestore" 
    ==> "Build"
    ==> "Package"

// start build
Target.runOrDefault "Build"
