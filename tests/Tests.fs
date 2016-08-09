module erecruit.Maybe.Tests
open erecruit.Utils
open Swensen.Unquote
open Fuchu.Xunit
open Fuchu

let t = Swensen.Unquote.Assertions.test
  
let [<FuchuTests>] tests() = 
  TestList [

    test "Dereferencing Nothing-kind computation should throw a NoValueException" {
      let m = Maybe.Nothing<int>()
      raises<Maybe.NoValueException> <@ m.Value @>
    }

    test "Dereferencing Error/exception-kind computation should throw a ComputationErrorException" {
      let ex = new System.ArgumentException("Boo!")
      let m = Maybe.Fail<int>( Maybe.Error( ex ) )
      try 
        m.Value |> ignore
      with 
        | :? Maybe.ComputationErrorException as e -> 
            t <@ e.InnerException = (ex :> _) @> 
            t <@ e.Message = "An exception was thrown during a Maybe computation." @>
        | e -> 
            t <@ e.GetType().Name = "" @>
    }

    test "Dereferencing Error/exception-kind computation should throw a ComputationErrorException" {
      let m = Maybe.Fail<int>( Maybe.Error( "Boo", "Hoo" ) )
      try 
        m.Value |> ignore
      with 
        | :? Maybe.ComputationErrorException as e -> 
            t <@ e.InnerException = null @> 
            t <@ e.Message = "Boo, Hoo" @>
        | e -> 
            t <@ e.GetType().Name = "" @>
    }

  ]