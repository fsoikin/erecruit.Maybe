#r @"packages/build/FAKE/tools/FakeLib.dll"
open Fake
open System.IO

let config = getBuildParamOrDefault "Config" "Debug"
let version = getBuildParamOrDefault "Version" "0.0.0.0"
let nugetApiKey = getBuildParam "NugetApiKey"

let versionRegex = System.Text.RegularExpressions.Regex("""(?<=(AssemblyVersion|AssemblyFileVersion)\(\")[\d\.]+(?=\"\))""")

let patchVersion() =
  let file = "./src/Properties/AssemblyInfo.cs"
  let text = File.ReadAllText file
  let text = versionRegex.Replace( text, version )
  File.WriteAllText( file, text )

let build target () =
  MSBuildHelper.build 
    (fun p -> { p with
                  Targets = [target]
                  Properties = ["Configuration", config]
                  Verbosity = Some MSBuildVerbosity.Minimal })
    "./erecruit.Maybe.sln"

let publish() =
  Paket.Pack (fun p -> { p with 
                          TemplateFile = !! "paket.template" |> Seq.head 
                          BuildConfig = config
                          OutputPath = "./nupkg" } )
  Paket.Push (fun p -> { p with
                          WorkingDir = "./nupkg"
                          ApiKey = nugetApiKey } )

Target "Build" <| fun _ -> patchVersion(); build "Build" ()
Target "Clean" <| build "Clean"
Target "Rebuild" DoNothing
Target "RunTests" DoNothing 
Target "Publish" <| publish

"Build" ==> "Rebuild"
"Clean" ==> "Rebuild"
"Clean" ?=> "Build"

"Build" ==> "RunTests" ==> "Publish"

RunTargetOrDefault "Build"