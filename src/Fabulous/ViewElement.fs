﻿// Copyright 2018-2019 Fabulous contributors. See LICENSE.md for license.
namespace Fabulous

#nowarn "67" // cast always holds

open System
open System.Collections.Generic
open System.Diagnostics

[<AutoOpen>]
module internal AttributeKeys =
    let attribKeys = Dictionary<string,int>()
    let attribNames = Dictionary<int,string>()

[<Struct>]
type AttributeKey<'T> internal (keyv: int) =

    static let getAttribKeyValue (attribName: string) : int =
        match attribKeys.TryGetValue(attribName) with
        | true, keyv -> keyv
        | false, _ ->
            let keyv = attribKeys.Count + 1
            attribKeys.[attribName] <- keyv
            attribNames.[keyv] <- attribName
            keyv

    new (keyName: string) = AttributeKey<'T>(getAttribKeyValue keyName)

    member __.KeyValue = keyv

    member __.Name = AttributeKey<'T>.GetName(keyv)

    static member GetName(keyv: int) =
        match attribNames.TryGetValue(keyv) with
        | true, keyv -> keyv
        | false, _ -> failwithf "unregistered attribute key %d" keyv


/// A description of a visual element
type AttributesBuilder (attribCount: int) =

    let mutable count = 0
    let mutable attribs = Array.zeroCreate<KeyValuePair<int, obj>>(attribCount)

    /// Get the attributes of the visual element
    [<DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>]
    member __.Attributes =
        if isNull attribs then [| |]
        else attribs |> Array.map (fun kvp -> KeyValuePair(AttributeKey<int>.GetName kvp.Key, kvp.Value))

    /// Get the attributes of the visual element
    member __.Close() : _[] =
        let res = attribs
        attribs <- null
        res

    /// Produce a new visual element with an adjusted attribute
    member __.Add(key: AttributeKey<'T>, value: 'T) =
        if isNull attribs then failwithf "The attribute builder has already been closed"
        if count >= attribs.Length then failwithf "The attribute builder was not large enough for the added attributes, it was given size %d. Did you get the attribute count right?" attribs.Length
        attribs.[count] <- KeyValuePair(key.KeyValue, box value)
        count <- count + 1


type ViewRef(onAttached, onDetached) =
    let handle = System.WeakReference<obj>(null)

    /// Check if the new target is the same than the previous one
    /// This is done to avoid triggering change events when nothing changes
    member private __.IsSameTarget(target) =
        match handle.TryGetTarget() with
        | true, res when res = target -> true
        | _ -> false

    member x.Set(target: obj) : unit =
        if not (x.IsSameTarget(target)) then
            handle.SetTarget(target)
            onAttached x target

    member x.Unset() : unit =
        if not (x.IsSameTarget(null)) then
            handle.SetTarget(null)
            onDetached x ()

    member __.TryValue =
        match handle.TryGetTarget() with
        | true, null -> None
        | true, res -> Some res
        | _ -> None

type ViewRef<'T when 'T : not struct>() =
    let attached = Event<EventHandler<'T>, 'T>()
    let detached = Event<EventHandler, EventArgs>()

    let onAttached sender target = attached.Trigger(sender, unbox target)
    let onDetached sender () = detached.Trigger(sender, EventArgs())

    let handle = ViewRef(onAttached, onDetached)

    [<CLIEvent>] member __.Attached = attached.Publish
    [<CLIEvent>] member __.Detached = detached.Publish

    member __.Value : 'T =
        match handle.TryValue with
        | Some res -> unbox res
        | None -> failwith "view reference target has been collected or was not set"

    member __.TryValue : 'T option =
        match handle.TryValue with
        | Some res -> Some (unbox res)
        | None -> None

    member __.Unbox = handle

/// A description of a visual element
type ViewElement internal (targetType: Type, create: (ViewElement -> obj), update: (ViewElement voption -> ViewElement -> obj -> unit), updateAttachedProperties: (int -> ViewElement voption -> ViewElement -> obj -> unit), attribs: KeyValuePair<int,obj>[]) =

    // Recursive search of an attribute by its key.
    // Perf note: This is preferred to Array.tryFind because it avoids capturing the context with a lambda
    let tryFindAttrib key =
        let rec tryFindAttribRec key i =
            if i >= attribs.Length then
                ValueNone
            elif attribs.[i].Key = key then
                ValueSome (attribs.[i])
            else
                tryFindAttribRec key (i + 1)
        tryFindAttribRec key 0

    new (targetType: Type, create: (ViewElement -> obj), update: (ViewElement voption -> ViewElement -> obj -> unit), updateAttachedProperties: (int -> ViewElement voption -> ViewElement -> obj -> unit), attribsBuilder: AttributesBuilder) =
        ViewElement(targetType, create, update, updateAttachedProperties, attribsBuilder.Close())

    static member Create
            (create: (ViewElement -> 'T),
             update: (ViewElement voption -> ViewElement -> 'T -> unit),
             updateAttachedProperties: (int -> ViewElement voption -> ViewElement -> obj -> unit),
             attribsBuilder: AttributesBuilder) =

        ViewElement(
            typeof<'T>, (create >> box),
            (fun prev curr target -> update prev curr (unbox target)),
            updateAttachedProperties,
            attribsBuilder.Close()
        )

    static member val CreatedAttribKey : AttributeKey<obj -> unit> = AttributeKey<_>("Created")
    static member val RefAttribKey : AttributeKey<ViewRef> = AttributeKey<_>("Ref")
    static member val KeyAttribKey : AttributeKey<string> = AttributeKey<_>("Key")

    /// Get the type created by the visual element
    member x.TargetType = targetType

    /// Get the attributes of the visual element
    [<DebuggerBrowsable(DebuggerBrowsableState.Never)>]
    member x.AttributesKeyed = attribs

    /// Get the attributes of the visual element
    [<DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>]
    member x.Attributes = attribs |> Array.map (fun kvp -> KeyValuePair(AttributeKey<int>.GetName kvp.Key, kvp.Value))

    /// Get an attribute of the visual element
    member x.TryGetAttributeKeyed<'T>(key: AttributeKey<'T>) =
        match tryFindAttrib key.KeyValue with
        | ValueSome kvp -> ValueSome(unbox<'T>(kvp.Value))
        | ValueNone -> ValueNone

    /// Get an attribute of the visual element
    member x.TryGetAttribute<'T>(name: string) =
        x.TryGetAttributeKeyed<'T>(AttributeKey<'T> name)

    /// Get an attribute of the visual element
    member x.GetAttributeKeyed<'T>(key: AttributeKey<'T>) =
        match tryFindAttrib key.KeyValue with
        | ValueSome kvp -> unbox<'T>(kvp.Value)
        | ValueNone -> failwithf "Property '%s' does not exist on %s" key.Name x.TargetType.Name

    /// Try get the key attribute value
    member x.TryGetKey() = x.TryGetAttributeKeyed(ViewElement.KeyAttribKey)

    /// Differentially update a visual element and update the ViewRefs if present
    member private x.Update(prevOpt: ViewElement voption, curr: ViewElement, target: obj) =
        let prevViewRefOpt = match prevOpt with ValueNone -> ValueNone | ValueSome prev -> prev.TryGetAttributeKeyed(ViewElement.RefAttribKey)
        let currViewRefOpt = curr.TryGetAttributeKeyed(ViewElement.RefAttribKey)

        // To avoid triggering unwanted events, don't unset if prevOpt = None or ViewRef is the same instance for prev and curr
        match struct (prevViewRefOpt, currViewRefOpt) with
        | struct (ValueSome prevViewRef, ValueSome currViewRef) when Object.ReferenceEquals(prevViewRef, currViewRef) -> ()
        | struct (ValueSome prevViewRef, _) -> prevViewRef.Unset()
        | _ -> ()

        update prevOpt curr target

        match currViewRefOpt with
        | ValueNone -> ()
        | ValueSome currViewRef -> currViewRef.Set(target)

    /// Apply initial settings to a freshly created visual element
    member x.Update (target: obj) = x.Update(ValueNone, x, target)

    /// Differentially update a visual element given the previous settings
    member x.UpdateIncremental(prev: ViewElement, target: obj) = x.Update(ValueSome prev, x, target)

    /// Differentially update the inherited attributes of a visual element given the previous settings
    member x.UpdateInherited(prevOpt: ViewElement voption, curr: ViewElement, target: obj) = x.Update(prevOpt, curr, target)

    /// Differentially update the attached properties of a child of a collection
    member x.UpdateAttachedPropertiesForAttribute<'T>(attributeKey: AttributeKey<'T>, prevOpt: ViewElement voption, curr: ViewElement, target: obj) = updateAttachedProperties attributeKey.KeyValue prevOpt curr target

    /// Create the UI element from the view description
    member x.Create() : obj =
        Debug.WriteLine (sprintf "Create %O" x.TargetType)
        let target = create x
        x.Update(ValueNone, x, target)
        match x.TryGetAttributeKeyed(ViewElement.CreatedAttribKey) with
        | ValueSome f -> f target
        | ValueNone -> ()
        target

    /// Produce a new visual element with an adjusted attribute
    member __.WithAttribute(key: AttributeKey<'T>, value: 'T) =
        let duplicateViewElement newAttribsLength attribIndex =
            let attribs2 = Array.zeroCreate newAttribsLength
            Array.blit attribs 0 attribs2 0 attribs.Length
            attribs2.[attribIndex] <- KeyValuePair(key.KeyValue, box value)
            ViewElement(targetType, create, update, updateAttachedProperties, attribs2)

        let n = attribs.Length

        let existingAttrIndexOpt = attribs |> Array.tryFindIndex (fun attr -> attr.Key = key.KeyValue)
        match existingAttrIndexOpt with
        | Some i ->
            duplicateViewElement n i // duplicate and replace existing attribute
        | None ->
            duplicateViewElement (n + 1) n // duplicate and add new attribute

    /// Produce a new visual element without an adjusted attribute
    member x.RemoveAttribute(key: AttributeKey<'T>) =
        let existingAttrIndexOpt = attribs |> Array.tryFindIndex (fun attr -> attr.Key = key.KeyValue)
        match existingAttrIndexOpt with
        | Some _ ->
            let attribs2 = attribs |> Array.filter (fun x -> x.Key <> key.KeyValue)
            true, ViewElement(targetType, create, update, updateAttachedProperties, attribs2)
        | None ->
            false, x

    override x.ToString() = sprintf "%s(...)@%d" x.TargetType.Name (x.GetHashCode())



