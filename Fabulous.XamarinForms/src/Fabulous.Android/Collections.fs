namespace Fabulous.Android

open Android.Views
open Fabulous
open System.Collections
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Buffers

/// This module contains the update logic for the controls with children
module Collections =
  
    [<Struct>]
    type Operation<'T> =
        | Insert of insertIndex: int *  element: 'T
        | Move of moveOldIndex: int * moveNewIndex: int
        | Update of updateIndex: int * updatePrev: 'T * updateCurr: 'T
        | MoveAndUpdate of moveAndUpdateOldIndex: int * moveAndUpdateprev: 'T * moveAndUpdatenewIndex: int * moveAndUpdatecurr: 'T
        | Delete of deleteOldIndex: int

    let isMatch canReuse aggressiveReuseMode currIndex newChild (struct (index, reusableChild)) =
        canReuse reusableChild newChild
        && (aggressiveReuseMode || index = currIndex)

    let rec tryFindRec canReuse aggressiveReuseMode currIndex newChild (reusableElements: struct (int * 'T)[]) (reusableElementsCount: int) index =
        if index >= reusableElementsCount then
            ValueNone
        elif isMatch canReuse aggressiveReuseMode currIndex newChild reusableElements.[index] then
            let struct (prevIndex, prevChild) = reusableElements.[index]
            ValueSome (struct (index, prevIndex, prevChild))
        else
            tryFindRec canReuse aggressiveReuseMode currIndex newChild reusableElements reusableElementsCount (index + 1)

    let tryFindReusableElement canReuse aggressiveReuseMode currIndex newChild reusableElements reusableElementsCount =
        tryFindRec canReuse aggressiveReuseMode currIndex newChild reusableElements reusableElementsCount 0

    let deleteAt index (arr: 'T[]) arrCount =
        if index + 1 >= arrCount then
            ()
        else
            for i = index to arrCount - 1 do
                arr.[i] <- arr.[i + 1]

    let rec canReuseChildOfRec keyOf canReuse prevChild (coll: 'T[]) key i =
        if i >= coll.Length then
            false
        elif keyOf coll.[i] = ValueSome key && canReuse prevChild coll.[i] then
            true
        else
            canReuseChildOfRec keyOf canReuse prevChild coll key (i + 1)

    let canReuseChildOf keyOf canReuse prevChild (coll: 'T[]) key =
        canReuseChildOfRec keyOf canReuse prevChild coll key 0

    let rec isIdenticalRec identical prevChild (coll: 'T[]) i =
        if i >= coll.Length then
            false
        elif identical prevChild coll.[i] then
            true
        else
            isIdenticalRec identical prevChild coll (i + 1)
   
    let isIdentical identical prevChild (coll: 'T[]) =
        isIdenticalRec identical prevChild coll 0

    /// Returns a list of operations to apply to go from the initial list to the new list
    ///
    /// The algorithm will try in priority to update elements sharing the same instance (usually achieved with dependsOn)
    /// or sharing the same key. All other elements will try to reuse previous elements where possible.
    /// If no reuse is possible, the element will create a new control.
    ///
    /// In aggressive reuse mode, the algorithm will try to reuse any reusable element.
    /// In the non-aggressive reuse mode, the algorithm will try to reuse a reusable element only if it is at the same index
    let diff<'T when 'T : equality>
            (aggressiveReuseMode: bool)
            (prevCollLength: int)
            (prevCollOpt: 'T[] voption)
            (coll: 'T[])
            (keyOf: 'T -> string voption)
            (canReuse: 'T -> 'T -> bool)

            (workingSet: Operation<'T>[])
        =
        let mutable workingSetIndex = 0

        // Separate the previous elements into 4 lists
        // The ones whose instances have been reused (dependsOn)
        // The ones whose keys have been reused and should be updated
        // The ones whose keys have not been reused and should be discarded
        // The rest which can be reused by any other element
        let identicalElements = Dictionary<'T, int>()
        let keyedElements = Dictionary<string, struct (int * 'T)>()
        let reusableElements = ArrayPool<struct (int * 'T)>.Shared.Rent(prevCollLength)
        let discardedElements = ArrayPool<int>.Shared.Rent(prevCollLength)

        let mutable reusableElementsCount = 0
        let mutable discardedElementsCount = 0

        if prevCollOpt.IsSome && prevCollOpt.Value.Length > 0 then
            for prevIndex in 0 .. prevCollOpt.Value.Length - 1 do
                let prevChild = prevCollOpt.Value.[prevIndex]
                if isIdentical identical prevChild coll then
                    identicalElements.Add(prevChild, prevIndex) |> ignore
                else
                    match keyOf prevChild with
                    | ValueSome key when canReuseChildOf keyOf canReuse prevChild coll key ->
                        keyedElements.Add(key, struct (prevIndex, prevChild))
                    | ValueNone ->
                        reusableElements.[reusableElementsCount] <- struct (prevIndex, prevChild)
                        reusableElementsCount <- reusableElementsCount + 1
                    | ValueSome _ ->
                        discardedElements.[discardedElementsCount] <- prevIndex
                        discardedElementsCount <- discardedElementsCount + 1

        for i in 0 .. coll.Length - 1 do
            let newChild = coll.[i]

            // Check if the same instance was reused (dependsOn), if so just move the element to the correct index
            match identicalElements.TryGetValue(newChild) with
            | (true, prevIndex) ->
                if prevIndex <> i then
                    workingSet.[workingSetIndex] <- Move (prevIndex, i)
                    workingSetIndex <- workingSetIndex + 1
            | _ ->
                // If the key existed previously, reuse the previous element
                match keyOf newChild with
                | ValueSome key when keyedElements.ContainsKey(key) ->
                    let struct (prevIndex, prevChild) = keyedElements.[key]
                    if prevIndex <> i then
                        workingSet.[workingSetIndex] <- MoveAndUpdate (prevIndex, prevChild, i, newChild)
                        workingSetIndex <- workingSetIndex + 1
                    else
                        workingSet.[workingSetIndex] <- Update (i, prevChild, newChild)
                        workingSetIndex <- workingSetIndex + 1

                // Otherwise, reuse an old element if possible or create a new one
                | _ ->
                    match tryFindReusableElement canReuse aggressiveReuseMode i newChild reusableElements reusableElementsCount with
                    | ValueSome (struct (reusableIndex, prevIndex, prevChild)) ->
                        deleteAt reusableIndex reusableElements reusableElementsCount
                        reusableElementsCount <- reusableElementsCount - 1
                        if prevIndex <> i then
                            workingSet.[workingSetIndex] <- MoveAndUpdate (prevIndex, prevChild, i, newChild)
                            workingSetIndex <- workingSetIndex + 1
                        else
                            workingSet.[workingSetIndex] <- Update (i, prevChild, newChild)
                            workingSetIndex <- workingSetIndex + 1

                    | ValueNone ->
                        workingSet.[workingSetIndex] <- Insert (i, newChild)
                        workingSetIndex <- workingSetIndex + 1

        // If we have discarded elements, delete them
        if discardedElementsCount > 0 then
            for i = 0 to discardedElementsCount - 1 do
                workingSet.[workingSetIndex] <- Delete discardedElements.[i]
                workingSetIndex <- workingSetIndex + 1

        // If we still have old elements that were not reused, delete them
        if reusableElementsCount > 0 then
            for i = 0 to reusableElementsCount - 1 do
                let struct (prevIndex, _) = reusableElements.[i]
                workingSet.[workingSetIndex] <- Delete prevIndex
                workingSetIndex <- workingSetIndex + 1

        ArrayPool<struct (int * 'T)>.Shared.Return(reusableElements)
        ArrayPool<int>.Shared.Return(discardedElements)

        workingSetIndex

    // Shift all old indices by 1 (down the list) on insert after the inserted position
    let shiftForInsert (prevIndices: int[]) index =
        for i in 0 .. prevIndices.Length - 1 do
            if prevIndices.[i] >= index then
                prevIndices.[i] <- prevIndices.[i] + 1

    // Shift all old indices by -1 (up the list) on delete after the deleted position
    let shiftForDelete (prevIndices: int[]) originalIndexInPrevColl prevIndex =
        for i in 0 .. prevIndices.Length - 1 do
            if prevIndices.[i] > prevIndex then
                prevIndices.[i] <- prevIndices.[i] - 1
        prevIndices.[originalIndexInPrevColl] <- -1

    // Shift all old indices between the previous and new position on move
    let shiftForMove (prevIndices: int[]) originalIndexInPrevColl prevIndex newIndex =
        for i in 0 .. prevIndices.Length - 1 do
            if prevIndex < prevIndices.[i] && prevIndices.[i] <= newIndex then
                prevIndices.[i] <- prevIndices.[i] - 1
            else if newIndex <= prevIndices.[i] && prevIndices.[i] < prevIndex then
                prevIndices.[i] <- prevIndices.[i] + 1
        prevIndices.[originalIndexInPrevColl] <- newIndex

    // Return an update operation preceded by a move only if actual indices don't match
    let moveAndUpdate (prevIndices: int[]) oldIndex prev newIndex curr =
        let prevIndex = prevIndices.[oldIndex]
        if prevIndex = newIndex then
            Update (newIndex, prev, curr)
        else
            shiftForMove prevIndices oldIndex prevIndex newIndex
            MoveAndUpdate (prevIndex, prev, newIndex, curr)

    /// Reduces the operations of the DiffResult to be applicable to an ObservableCollection.
    ///
    /// diff returns all the operations to move from List A to List B.
    /// Except with ObservableCollection, we're forced to apply the changes one after the other, changing the indices
    /// So this algorithm compensates this offsetting
    let adaptDiffForObservableCollection (prevCollLength: int) (workingSet: Operation<'T>[]) (workingSetIndex: int) =
        let prevIndices = Array.init prevCollLength id

        let mutable position = 0

        for i = 0 to workingSetIndex - 1 do
            match workingSet.[i] with
            | Insert (index, element) ->
                workingSet.[position] <- Insert (index, element)
                position <- position + 1
                shiftForInsert prevIndices index

            | Move (oldIndex, newIndex) ->
                // Prevent a move if the actual indices match
                let prevIndex = prevIndices.[oldIndex]
                if prevIndex <> newIndex then
                    workingSet.[position] <- (Move (prevIndex, newIndex))
                    position <- position + 1
                    shiftForMove prevIndices oldIndex prevIndex newIndex

            | Update (index, prev, curr) ->
                workingSet.[position] <- moveAndUpdate prevIndices index prev index curr
                position <- position + 1

            | MoveAndUpdate (oldIndex, prev, newIndex, curr) ->
                workingSet.[position] <- moveAndUpdate prevIndices oldIndex prev newIndex curr
                position <- position + 1

            | Delete oldIndex ->
                let prevIndex = prevIndices.[oldIndex]
                workingSet.[position] <- Delete prevIndex
                position <- position + 1
                shiftForDelete prevIndices oldIndex prevIndex

        position

    /// Incremental list maintenance: given a collection, and a previous version of that collection, perform
    /// a reduced number of clear/add/remove/insert operations
    let updateViewGroupCollection
           (aggressiveReuseMode: bool)
           (prevCollOpt: 'T[] voption)
           (collOpt: 'T[] voption)
           (targetColl: ViewGroup)
           (keyOf: 'T -> string voption)
           (canReuse: 'T -> 'T -> bool)
           (create: 'T -> 'TargetT)
           (update: 'T -> 'T -> View -> unit) // Incremental element-wise update, only if element reuse is allowed
           (attach: 'T voption -> 'T -> obj -> unit) // adjust attached properties
        =

        match struct (prevCollOpt, collOpt) with
        | struct (ValueNone, ValueNone) -> ()
        | struct (ValueSome prevColl, ValueSome newColl) when identical prevColl newColl -> ()
        | struct (ValueSome prevColl, ValueSome newColl) when prevColl <> null && newColl <> null && prevColl.Length = 0 && newColl.Length = 0 -> ()
        | struct (ValueSome _, ValueNone) -> targetColl.RemoveAllViews()
        | struct (ValueSome _, ValueSome coll) when (coll = null || coll.Length = 0) -> targetColl.RemoveAllViews()
        | struct (_, ValueSome coll) ->
            let prevCollLength = (match prevCollOpt with ValueNone -> 0 | ValueSome c -> c.Length)
            let workingSet = ArrayPool<Operation<'T>>.Shared.Rent(prevCollLength + coll.Length)

            let operationsCount =
                diff aggressiveReuseMode prevCollLength prevCollOpt coll keyOf canReuse workingSet
                |> adaptDiffForObservableCollection prevCollLength workingSet

            for i = 0 to operationsCount - 1 do
                match workingSet.[i] with
                | Insert (index, element) ->
                    let child = create element
                    attach ValueNone element child
                    targetColl.AddView(child, index)

                | Move (prevIndex, newIndex) ->
                    let child = targetColl.GetChildAt prevIndex
                    targetColl.RemoveViewAt(prevIndex)
                    targetColl.AddView(child, newIndex)

                | Update (index, prev, curr) ->
                    let child = targetColl.GetChildAt index
                    update prev curr child
                    attach (ValueSome prev) curr child

                | MoveAndUpdate (prevIndex, prev, newIndex, curr) ->
                    let child = targetColl.GetChildAt prevIndex
                    targetColl.RemoveViewAt(prevIndex)
                    targetColl.AddView(child, newIndex)
                    update prev curr child
                    attach (ValueSome prev) curr child

                | Delete index ->
                    targetColl.RemoveViewAt(index) |> ignore

            ArrayPool<Operation<'T>>.Shared.Return(workingSet)

    let updateViewGroupChildren2 prevCollOpt collOpt target create update attach =
        updateViewGroupCollection true prevCollOpt collOpt target ViewHelpers.tryGetKey ViewHelpers.canReuseView create update attach

    /// Update a control given the previous and new view elements
    let inline updateChild (prevChild: ViewElement) (newChild: ViewElement) targetChild =
        newChild.UpdateIncremental(prevChild, targetChild)
        
    let updateViewGroupChildren prevCollOpt collOpt (target: ViewGroup) attach =
        updateViewGroupChildren2 prevCollOpt collOpt target (fun c ->
            c.Create(AndroidContextHost(target.Context)) :?> View) updateChild attach