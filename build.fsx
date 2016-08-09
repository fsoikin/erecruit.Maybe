#r @"packages/build/FAKE/tools/FakeLib.dll"
#load "./build/Publish.fsx"
#load "./build/PatchVersion.fsx"
open Fake

let config = getBuildParamOrDefault "Config" "Debug"

let build target () =
  MSBuildHelper.build 
    (fun p -> { p with
                  Targets = [target]
                  Properties = ["Configuration", config]
                  Verbosity = Some MSBuildVerbosity.Minimal })
    "./erecruit.Maybe.sln"

Target "Build" <| fun _ -> PatchVersion.patchVersion "./src/Properties/AssemblyInfo.cs"; build "Build" ()
Target "Clean" <| build "Clean"
Target "Rebuild" DoNothing
Target "RunTests" <| fun _ -> [sprintf "./tests/bin/%s/erecruit.Maybe.Tests.dll" config] |> Fake.Testing.XUnit2.xUnit2 id 
Target "Publish" <| Publish.publishPackage config "./src/paket.template"

"Build" ==> "Rebuild"
"Clean" ==> "Rebuild"
"Clean" ?=> "Build"

"Build" ==> "RunTests" ==> "Publish"

RunTargetOrDefault "Build"