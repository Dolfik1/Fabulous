// Copyright 2018-2019 Fabulous contributors. See LICENSE.md for license.
namespace Fabulous.Android

open Fabulous
open System
open System.Collections.Concurrent
open System.Threading

[<AutoOpen>]
module ViewHelpers =
    
    /// Checks whether two objects are reference-equal
    let identical (x: 'T) (y:'T) = System.Object.ReferenceEquals(x, y)

    let identicalVOption (x: 'T voption) (y: 'T voption) =
        match struct (x, y) with
        | struct (ValueNone, ValueNone) -> true
        | struct (ValueSome x1, ValueSome y1) when identical x1 y1 -> true
        | _ -> false
    
    let canReuseKey (prevChild: ViewElement) (newChild: ViewElement) =
        let prevAutomationId = prevChild.TryGetAttribute<string>("Key")
        let newAutomationId = newChild.TryGetAttribute<string>("Key")

        match prevAutomationId with
        | ValueSome _ when prevAutomationId <> newAutomationId -> false
        | _ -> true
        
    let rec canReuseView (prevChild: ViewElement) (newChild: ViewElement) =
        if prevChild.TargetType = newChild.TargetType && canReuseKey prevChild newChild then
            true
        else
            false
            
    /// Try to retrieve the value of the "Key" property
    let tryGetKey (x: ViewElement) = x.TryGetKey()