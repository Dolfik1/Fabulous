namespace Fabulous.Android

open Android.App
open Fabulous


type IAndroidHost =
    abstract Context: Android.Content.Context
    inherit IHost

type AndroidContextHost(context) =
    interface IAndroidHost with
        member x.Context = context
    
    interface IHost with
        member __.GetRootView() = failwith "Not supported!"
        member __.SetRootView(rootView) = failwith "Not supported!"

type AndroidActivityHost(activity: Activity) =
    interface IAndroidHost with
        member x.Context = activity :> Android.Content.Context
    
    interface IHost with
        member __.GetRootView() =
            activity.Window.DecorView.RootView :> obj

        member __.SetRootView(rootView) =
            activity.SetContentView(rootView :?> Android.Views.View)

