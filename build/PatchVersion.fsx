(*
  This is a reusable chunk of FAKE script for rigging library version during build.
  
  patchVersion assemblyInfoFile 
      finds AssemblyVersion and AssemblyFileVerison attributes
      in the given file and replaces their version with one
      specified with the "Version" build argument.
*)
open Fake
open System.IO

let version = getBuildParamOrDefault "Version" "0.0.0.0"
let versionRegex = System.Text.RegularExpressions.Regex("""(?<=(AssemblyVersion|AssemblyFileVersion)\(\")[\d\.]+(?=\"\))""")

let patchVersion assemblyInfoFile =
  let text = File.ReadAllText assemblyInfoFile
  let text = versionRegex.Replace( text, version )
  File.WriteAllText( assemblyInfoFile, text )
