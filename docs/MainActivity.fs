namespace Fabulous.Android.Counter

open System

open Android.App
open Fabulous
open Fabulous.Android


module MainActivity = 
    type Model = 
      { Count : int 
        Step : int
        TimerOn: bool }

    type Msg = 
        | Increment 
        | Decrement 
        | Reset
        | SetStep of int
        | TimerToggled of bool
        | TimedTick

    type CmdMsg =
        | TickTimer

    let timerCmd () =
        async { do! Async.Sleep 200
                return TimedTick }
        |> Cmd.ofAsyncMsg

    let mapCmdMsgToCmd cmdMsg =
        match cmdMsg with
        | TickTimer -> timerCmd()

    let initModel () = { Count = 0; Step = 1; TimerOn=false }

    let init () = initModel () , []

    let update msg model =
        match msg with
        | Increment -> { model with Count = model.Count + model.Step }, []
        | Decrement -> { model with Count = model.Count - model.Step }, []
        | Reset -> init ()
        | SetStep n -> { model with Step = n }, []
        | TimerToggled on -> { model with TimerOn = on }, (if on then [ TickTimer ] else [])
        | TimedTick -> if model.TimerOn then { model with Count = model.Count + model.Step }, [ TickTimer ] else model, [] 

    let view (model: Model) dispatch =  
        View.TextView(
            text = "Hello, world!"
        )

[<Activity (Label = "Fabulous.Android.Counter", MainLauncher = true, Icon = "@mipmap/icon")>]
type MainActivity () =
    inherit Activity ()

    let mutable count:int = 1

    override this.OnCreate (bundle) =

        base.OnCreate (bundle)
        
        Program.mkProgramWithCmdMsg MainActivity.init MainActivity.update MainActivity.view MainActivity.mapCmdMsgToCmd
        |> Program.withConsoleTrace
        |> AndroidProgram.run this
        |> ignore

