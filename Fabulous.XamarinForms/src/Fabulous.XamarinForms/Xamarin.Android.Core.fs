// Copyright 2018-2020 Fabulous contributors. See LICENSE.md for license.
namespace 

#nowarn "59" // cast always holds
#nowarn "66" // cast always holds
#nowarn "67" // cast always holds
#nowarn "760"

open Fabulous

module ViewAttributes =

type ViewBuilders() =
[<AbstractClass; Sealed>]
type View private () =

[<AutoOpen>]
module ViewElementExtensions = 

    type ViewElement with

        member inline viewElement.With() =
            viewElement

