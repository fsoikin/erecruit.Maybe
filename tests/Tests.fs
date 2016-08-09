module erecruit.Maybe.Tests
open erecruit.Utils
open Swensen.Unquote
open Fuchu.Xunit
open Fuchu
  
let [<FuchuTests>] tests() = 
  TestList [

    test "Dereferencing Nothing-kind computation should throw a NoValueException" {
      let m = Maybe.Nothing<int>()
      raises<Maybe.NoValueException> <@ m.Value @>
    }

  ]