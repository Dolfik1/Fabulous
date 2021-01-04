// Copyright 2018-2019 Fabulous contributors. See LICENSE.md for license.

namespace Fabulous.Android

open Android.App
open Android.OS
open Fabulous

/// Program module - functions to manipulate program instances
[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AndroidProgram =    
    let private syncDispatch (dispatch: 'msg -> unit) =
        fun msg ->
            use handler = new Handler(Looper.MainLooper)
            handler.Post(fun _ -> dispatch msg) |> ignore
            
    let private syncAction (fn: unit -> unit) =
        fun () ->
            use handler = new Handler(Looper.MainLooper)
            handler.Post(fun _ -> fn ()) |> ignore
            
    let private setAndroidHandlers program =
        program
        // |> Program.withCanReuseView ViewHelpers.canReuseView
        |> Program.withSyncDispatch syncDispatch
        |> Program.withSyncAction syncAction

    /// Typical program, new commands are produced by `init` and `update` along with the new state.
    let mkProgram (init : 'arg -> 'model * Cmd<'msg>) (update : 'msg -> 'model -> 'model * Cmd<'msg>) (view : 'model -> Dispatch<'msg> -> ViewElement) =
        Fabulous.Program.mkProgram init update view
        |> setAndroidHandlers

    /// Simple program that produces only new state with `init` and `update`.
    let mkSimple (init : 'arg -> 'model) (update : 'msg -> 'model -> 'model) (view : 'model -> Dispatch<'msg> -> ViewElement) = 
        Fabulous.Program.mkSimple init update view
        |> setAndroidHandlers

    /// Typical program, new commands are produced discriminated unions returned by `init` and `update` along with the new state.
    let mkProgramWithCmdMsg (init: 'arg -> 'model * 'cmdMsg list) (update: 'msg -> 'model -> 'model * 'cmdMsg list) (view: 'model -> Dispatch<'msg> -> ViewElement) (mapToCmd: 'cmdMsg -> Cmd<'msg>) =
        Fabulous.Program.mkProgramWithCmdMsg init update view mapToCmd
        |> setAndroidHandlers

    let runWith activity arg program =
        let host = AndroidActivityHost(activity)

        program
        |> setAndroidHandlers // TODO: Kept for not breaking existing apps. Need to be removed later
        |> Program.runWithFabulous host arg
        
    let run app program =
        runWith app () program