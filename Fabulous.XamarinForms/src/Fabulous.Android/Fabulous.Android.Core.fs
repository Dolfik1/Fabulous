// Copyright 2018-2020 Fabulous contributors. See LICENSE.md for license.
namespace Fabulous.Android

#nowarn "59" // cast always holds
#nowarn "66" // cast always holds
#nowarn "67" // cast always holds
#nowarn "760"

open Fabulous

module ViewAttributes =
    let ClickAttribKey : AttributeKey<_> = AttributeKey<_>("Click")
    let ChildrenAttribKey : AttributeKey<_> = AttributeKey<_>("Children")
    let TextAttribKey : AttributeKey<_> = AttributeKey<_>("Text")

type ViewBuilders() =
    /// Builds the attributes for a View in the view
    static member inline BuildView(attribCount: int,
                                   ?ref: ViewRef,
                                   ?key: string,
                                   ?created: obj -> unit,
                                   ?click: unit -> unit) = 

        let attribCount = match ref with Some _ -> attribCount + 1 | None -> attribCount
        let attribCount = match key with Some _ -> attribCount + 1 | None -> attribCount
        let attribCount = match created with Some _ -> attribCount + 1 | None -> attribCount
        let attribCount = match click with Some _ -> attribCount + 1 | None -> attribCount

        let attribBuilder = AttributesBuilder(attribCount)
        match ref with None -> () | Some v -> attribBuilder.Add(Fabulous.ViewElement.RefAttribKey, (v)) 
        match key with None -> () | Some v -> attribBuilder.Add(Fabulous.ViewElement.KeyAttribKey, (v)) 
        match created with None -> () | Some v -> attribBuilder.Add(Fabulous.ViewElement.CreatedAttribKey, (v)) 
        match click with None -> () | Some v -> attribBuilder.Add(ViewAttributes.ClickAttribKey, (fun f -> System.EventHandler(fun _sender _args -> f()))(v)) 
        attribBuilder

    static member CreateView (host: IHost) : Android.Views.View =
        Android.Views.View((host :?> IAndroidHost).Context)

    static member UpdateViewAttachedProperties (propertyKey: int, prevOpt: ViewElement voption, curr: ViewElement, target: obj) = 
        ()

    static member UpdateView (prevOpt: ViewElement voption, curr: ViewElement, target: Android.Views.View) = 
        let mutable prevClickOpt = ValueNone
        let mutable currClickOpt = ValueNone
        for kvp in curr.AttributesKeyed do
            if kvp.Key = ViewAttributes.ClickAttribKey.KeyValue then 
                currClickOpt <- ValueSome (kvp.Value :?> System.EventHandler)
        match prevOpt with
        | ValueNone -> ()
        | ValueSome prev ->
            for kvp in prev.AttributesKeyed do
                if kvp.Key = ViewAttributes.ClickAttribKey.KeyValue then 
                    prevClickOpt <- ValueSome (kvp.Value :?> System.EventHandler)
        // Unsubscribe previous event handlers
        let shouldUpdateClick = not ((identicalVOption prevClickOpt currClickOpt))
        if shouldUpdateClick then
            match prevClickOpt with
            | ValueSome prevValue -> target.Click.RemoveHandler(prevValue)
            | ValueNone -> ()
        // Subscribe new event handlers
        if shouldUpdateClick then
            match currClickOpt with
            | ValueSome currValue -> target.Click.AddHandler(currValue)
            | ValueNone -> ()

    static member inline ConstructView(?ref: ViewRef<Android.Views.View>,
                                       ?key: string,
                                       ?created: (Android.Views.View -> unit),
                                       ?click: unit -> unit) = 

        let attribBuilder = ViewBuilders.BuildView(0,
                               ?ref=(match ref with None -> None | Some (ref: ViewRef<Android.Views.View>) -> Some ref.Unbox),
                               ?key=key,
                               ?created=(match created with None -> None | Some createdFunc -> Some (fun (target: obj) ->  createdFunc (unbox<Android.Views.View> target))),
                               ?click=click)

        ViewElement.Create<Android.Views.View>(ViewBuilders.CreateView, (fun prev curr target -> ViewBuilders.UpdateView(prev, curr, target)), (fun key prev curr target -> ViewBuilders.UpdateViewAttachedProperties(key, prev, curr, target)), attribBuilder)

    /// Builds the attributes for a ViewGroup in the view
    static member inline BuildViewGroup(attribCount: int,
                                        ?children: ViewElement list,
                                        ?ref: ViewRef,
                                        ?key: string,
                                        ?created: obj -> unit,
                                        ?click: unit -> unit) = 

        let attribCount = match children with Some _ -> attribCount + 1 | None -> attribCount

        let attribBuilder = ViewBuilders.BuildView(attribCount, ?ref=ref, ?key=key, ?created=created, ?click=click)
        match children with None -> () | Some v -> attribBuilder.Add(ViewAttributes.ChildrenAttribKey, Array.ofList(v)) 
        attribBuilder

    static member UpdateViewGroupAttachedProperties (propertyKey: int, prevOpt: ViewElement voption, curr: ViewElement, target: obj) = 
        ViewBuilders.UpdateViewAttachedProperties(propertyKey, prevOpt, curr, target)

    static member UpdateViewGroup (prevOpt: ViewElement voption, curr: ViewElement, target: Android.Views.ViewGroup) = 
        let mutable prevChildrenOpt = ValueNone
        let mutable currChildrenOpt = ValueNone
        for kvp in curr.AttributesKeyed do
            if kvp.Key = ViewAttributes.ChildrenAttribKey.KeyValue then 
                currChildrenOpt <- ValueSome (kvp.Value :?> ViewElement array)
        match prevOpt with
        | ValueNone -> ()
        | ValueSome prev ->
            for kvp in prev.AttributesKeyed do
                if kvp.Key = ViewAttributes.ChildrenAttribKey.KeyValue then 
                    prevChildrenOpt <- ValueSome (kvp.Value :?> ViewElement array)
        // Update inherited members
        ViewBuilders.UpdateView(prevOpt, curr, target)
        // Update properties
        Collections.updateViewGroupChildren prevChildrenOpt currChildrenOpt target
            (fun prevChildOpt currChild targetChild -> curr.UpdateAttachedPropertiesForAttribute(ViewAttributes.ChildrenAttribKey, prevChildOpt, currChild, targetChild))

    /// Builds the attributes for a TextView in the view
    static member inline BuildTextView(attribCount: int,
                                       ?text: string,
                                       ?ref: ViewRef,
                                       ?key: string,
                                       ?created: obj -> unit,
                                       ?click: unit -> unit) = 

        let attribCount = match text with Some _ -> attribCount + 1 | None -> attribCount

        let attribBuilder = ViewBuilders.BuildView(attribCount, ?ref=ref, ?key=key, ?created=created, ?click=click)
        match text with None -> () | Some v -> attribBuilder.Add(ViewAttributes.TextAttribKey, (v)) 
        attribBuilder

    static member CreateTextView (host: IHost) : Android.Widget.TextView =
        Android.Widget.TextView((host :?> IAndroidHost).Context)

    static member UpdateTextViewAttachedProperties (propertyKey: int, prevOpt: ViewElement voption, curr: ViewElement, target: obj) = 
        ViewBuilders.UpdateViewAttachedProperties(propertyKey, prevOpt, curr, target)

    static member UpdateTextView (prevOpt: ViewElement voption, curr: ViewElement, target: Android.Widget.TextView) = 
        let mutable prevTextOpt = ValueNone
        let mutable currTextOpt = ValueNone
        for kvp in curr.AttributesKeyed do
            if kvp.Key = ViewAttributes.TextAttribKey.KeyValue then 
                currTextOpt <- ValueSome (kvp.Value :?> string)
        match prevOpt with
        | ValueNone -> ()
        | ValueSome prev ->
            for kvp in prev.AttributesKeyed do
                if kvp.Key = ViewAttributes.TextAttribKey.KeyValue then 
                    prevTextOpt <- ValueSome (kvp.Value :?> string)
        // Update inherited members
        ViewBuilders.UpdateView(prevOpt, curr, target)
        // Update properties
        match struct (prevTextOpt, currTextOpt) with
        | struct (ValueSome prevValue, ValueSome currValue) when prevValue = currValue -> ()
        | struct (_, ValueSome currValue) -> target.Text <-  currValue
        | struct (ValueSome _, ValueNone) -> target.Text <- null
        | struct (ValueNone, ValueNone) -> ()

    static member inline ConstructTextView(?text: string,
                                           ?ref: ViewRef<Android.Widget.TextView>,
                                           ?key: string,
                                           ?created: (Android.Widget.TextView -> unit),
                                           ?click: unit -> unit) = 

        let attribBuilder = ViewBuilders.BuildTextView(0,
                               ?text=text,
                               ?ref=(match ref with None -> None | Some (ref: ViewRef<Android.Widget.TextView>) -> Some ref.Unbox),
                               ?key=key,
                               ?created=(match created with None -> None | Some createdFunc -> Some (fun (target: obj) ->  createdFunc (unbox<Android.Widget.TextView> target))),
                               ?click=click)

        ViewElement.Create<Android.Widget.TextView>(ViewBuilders.CreateTextView, (fun prev curr target -> ViewBuilders.UpdateTextView(prev, curr, target)), (fun key prev curr target -> ViewBuilders.UpdateTextViewAttachedProperties(key, prev, curr, target)), attribBuilder)

    /// Builds the attributes for a LinearLayout in the view
    static member inline BuildLinearLayout(attribCount: int,
                                           ?children: ViewElement list,
                                           ?ref: ViewRef,
                                           ?key: string,
                                           ?created: obj -> unit,
                                           ?click: unit -> unit) = 
        let attribBuilder = ViewBuilders.BuildViewGroup(attribCount, ?children=children, ?ref=ref, ?key=key, ?created=created, ?click=click)
        attribBuilder

    static member CreateLinearLayout (host: IHost) : Android.Widget.LinearLayout =
        Android.Widget.LinearLayout((host :?> IAndroidHost).Context)

    static member UpdateLinearLayoutAttachedProperties (propertyKey: int, prevOpt: ViewElement voption, curr: ViewElement, target: obj) = 
        ViewBuilders.UpdateViewGroupAttachedProperties(propertyKey, prevOpt, curr, target)

    static member UpdateLinearLayout (prevOpt: ViewElement voption, curr: ViewElement, target: Android.Widget.LinearLayout) = 
        ViewBuilders.UpdateViewGroup(prevOpt, curr, target)

    static member inline ConstructLinearLayout(?children: ViewElement list,
                                               ?ref: ViewRef<Android.Widget.LinearLayout>,
                                               ?key: string,
                                               ?created: (Android.Widget.LinearLayout -> unit),
                                               ?click: unit -> unit) = 

        let attribBuilder = ViewBuilders.BuildLinearLayout(0,
                               ?children=children,
                               ?ref=(match ref with None -> None | Some (ref: ViewRef<Android.Widget.LinearLayout>) -> Some ref.Unbox),
                               ?key=key,
                               ?created=(match created with None -> None | Some createdFunc -> Some (fun (target: obj) ->  createdFunc (unbox<Android.Widget.LinearLayout> target))),
                               ?click=click)

        ViewElement.Create<Android.Widget.LinearLayout>(ViewBuilders.CreateLinearLayout, (fun prev curr target -> ViewBuilders.UpdateLinearLayout(prev, curr, target)), (fun key prev curr target -> ViewBuilders.UpdateLinearLayoutAttachedProperties(key, prev, curr, target)), attribBuilder)

    /// Builds the attributes for a Button in the view
    static member inline BuildButton(attribCount: int,
                                     ?text: string,
                                     ?ref: ViewRef,
                                     ?key: string,
                                     ?created: obj -> unit,
                                     ?click: unit -> unit) = 
        let attribBuilder = ViewBuilders.BuildTextView(attribCount, ?text=text, ?ref=ref, ?key=key, ?created=created, ?click=click)
        attribBuilder

    static member CreateButton (host: IHost) : Android.Widget.Button =
        Android.Widget.Button((host :?> IAndroidHost).Context)

    static member UpdateButtonAttachedProperties (propertyKey: int, prevOpt: ViewElement voption, curr: ViewElement, target: obj) = 
        ViewBuilders.UpdateTextViewAttachedProperties(propertyKey, prevOpt, curr, target)

    static member UpdateButton (prevOpt: ViewElement voption, curr: ViewElement, target: Android.Widget.Button) = 
        ViewBuilders.UpdateTextView(prevOpt, curr, target)

    static member inline ConstructButton(?text: string,
                                         ?ref: ViewRef<Android.Widget.Button>,
                                         ?key: string,
                                         ?created: (Android.Widget.Button -> unit),
                                         ?click: unit -> unit) = 

        let attribBuilder = ViewBuilders.BuildButton(0,
                               ?text=text,
                               ?ref=(match ref with None -> None | Some (ref: ViewRef<Android.Widget.Button>) -> Some ref.Unbox),
                               ?key=key,
                               ?created=(match created with None -> None | Some createdFunc -> Some (fun (target: obj) ->  createdFunc (unbox<Android.Widget.Button> target))),
                               ?click=click)

        ViewElement.Create<Android.Widget.Button>(ViewBuilders.CreateButton, (fun prev curr target -> ViewBuilders.UpdateButton(prev, curr, target)), (fun key prev curr target -> ViewBuilders.UpdateButtonAttachedProperties(key, prev, curr, target)), attribBuilder)

/// Viewer that allows to read the properties of a ViewElement representing a View
type ViewViewer(element: ViewElement) =
    do if not ((typeof<Android.Views.View>).IsAssignableFrom(element.TargetType)) then failwithf "A ViewElement assignable to type 'Android.Views.View' is expected, but '%s' was provided." element.TargetType.FullName
    /// Get the value of the Key member
    member this.Key = element.GetAttributeKeyed(Fabulous.ViewElement.KeyAttribKey)
    /// Get the value of the Click member
    member this.Click = element.GetAttributeKeyed(ViewAttributes.ClickAttribKey)

/// Viewer that allows to read the properties of a ViewElement representing a ViewGroup
type ViewGroupViewer(element: ViewElement) =
    inherit ViewViewer(element)
    do if not ((typeof<Android.Views.ViewGroup>).IsAssignableFrom(element.TargetType)) then failwithf "A ViewElement assignable to type 'Android.Views.ViewGroup' is expected, but '%s' was provided." element.TargetType.FullName
    /// Get the value of the Children member
    member this.Children = element.GetAttributeKeyed(ViewAttributes.ChildrenAttribKey)

/// Viewer that allows to read the properties of a ViewElement representing a TextView
type TextViewViewer(element: ViewElement) =
    inherit ViewViewer(element)
    do if not ((typeof<Android.Widget.TextView>).IsAssignableFrom(element.TargetType)) then failwithf "A ViewElement assignable to type 'Android.Widget.TextView' is expected, but '%s' was provided." element.TargetType.FullName
    /// Get the value of the Text member
    member this.Text = element.GetAttributeKeyed(ViewAttributes.TextAttribKey)

/// Viewer that allows to read the properties of a ViewElement representing a LinearLayout
type LinearLayoutViewer(element: ViewElement) =
    inherit ViewGroupViewer(element)
    do if not ((typeof<Android.Widget.LinearLayout>).IsAssignableFrom(element.TargetType)) then failwithf "A ViewElement assignable to type 'Android.Widget.LinearLayout' is expected, but '%s' was provided." element.TargetType.FullName

/// Viewer that allows to read the properties of a ViewElement representing a Button
type ButtonViewer(element: ViewElement) =
    inherit TextViewViewer(element)
    do if not ((typeof<Android.Widget.Button>).IsAssignableFrom(element.TargetType)) then failwithf "A ViewElement assignable to type 'Android.Widget.Button' is expected, but '%s' was provided." element.TargetType.FullName

[<AbstractClass; Sealed>]
type View private () =
    /// Describes a View in the view
    static member inline View(?click: unit -> unit,
                              ?created: (Android.Views.View -> unit),
                              ?key: string,
                              ?ref: ViewRef<Android.Views.View>) =

        ViewBuilders.ConstructView(?click=click,
                               ?created=created,
                               ?key=key,
                               ?ref=ref)

    /// Describes a TextView in the view
    static member inline TextView(?click: unit -> unit,
                                  ?created: (Android.Widget.TextView -> unit),
                                  ?key: string,
                                  ?ref: ViewRef<Android.Widget.TextView>,
                                  ?text: string) =

        ViewBuilders.ConstructTextView(?click=click,
                               ?created=created,
                               ?key=key,
                               ?ref=ref,
                               ?text=text)

    /// Describes a LinearLayout in the view
    static member inline LinearLayout(?children: ViewElement list,
                                      ?click: unit -> unit,
                                      ?created: (Android.Widget.LinearLayout -> unit),
                                      ?key: string,
                                      ?ref: ViewRef<Android.Widget.LinearLayout>) =

        ViewBuilders.ConstructLinearLayout(?children=children,
                               ?click=click,
                               ?created=created,
                               ?key=key,
                               ?ref=ref)

    /// Describes a Button in the view
    static member inline Button(?click: unit -> unit,
                                ?created: (Android.Widget.Button -> unit),
                                ?key: string,
                                ?ref: ViewRef<Android.Widget.Button>,
                                ?text: string) =

        ViewBuilders.ConstructButton(?click=click,
                               ?created=created,
                               ?key=key,
                               ?ref=ref,
                               ?text=text)


[<AutoOpen>]
module ViewElementExtensions = 

    type ViewElement with

        /// Adjusts the Click property in the visual element
        member x.Click(value: unit -> unit) = x.WithAttribute(ViewAttributes.ClickAttribKey, (fun f -> System.EventHandler(fun _sender _args -> f()))(value))

        /// Adjusts the Key property in the visual element
        member x.Key(value: string) = x.WithAttribute(Fabulous.ViewElement.KeyAttribKey, (value))

        /// Adjusts the Children property in the visual element
        member x.Children(value: ViewElement list) = x.WithAttribute(ViewAttributes.ChildrenAttribKey, Array.ofList(value))

        /// Adjusts the Text property in the visual element
        member x.Text(value: string) = x.WithAttribute(ViewAttributes.TextAttribKey, (value))

        member inline viewElement.With(?click: unit -> unit, ?key: string, ?children: ViewElement list, ?text: string) =
            let viewElement = match click with None -> viewElement | Some opt -> viewElement.Click(opt)
            let viewElement = match key with None -> viewElement | Some opt -> viewElement.Key(opt)
            let viewElement = match children with None -> viewElement | Some opt -> viewElement.Children(opt)
            let viewElement = match text with None -> viewElement | Some opt -> viewElement.Text(opt)
            viewElement

    /// Adjusts the Click property in the visual element
    let click (value: unit -> unit) (x: ViewElement) = x.Click(value)
    /// Adjusts the Key property in the visual element
    let key (value: string) (x: ViewElement) = x.Key(value)
    /// Adjusts the Children property in the visual element
    let children (value: ViewElement list) (x: ViewElement) = x.Children(value)
    /// Adjusts the Text property in the visual element
    let text (value: string) (x: ViewElement) = x.Text(value)
