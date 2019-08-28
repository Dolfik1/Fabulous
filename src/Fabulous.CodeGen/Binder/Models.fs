namespace Fabulous.CodeGen.Binder

module Models =
    type IBoundConstructorMember =
        abstract ShortName: string
        abstract UniqueName: string
        abstract InputType: string
        abstract ConvertInputToModel: string
        abstract IsInherited: bool
        
    type IBoundMember =
        abstract UniqueName: string
        abstract InputType: string
        abstract ConvertInputToModel: string
    
    type BoundEvent =
        { Name: string
          ShortName: string
          UniqueName: string
          InputType: string
          ModelType: string
          ConvertInputToModel: string
          RelatedProperties: string array
          IsInherited: bool }
        interface IBoundConstructorMember with
            member this.ShortName = this.ShortName
            member this.UniqueName = this.UniqueName
            member this.InputType = this.InputType
            member this.ConvertInputToModel = this.ConvertInputToModel
            member this.IsInherited = this.IsInherited
        interface IBoundMember with
            member this.UniqueName = this.UniqueName
            member this.InputType = this.InputType
            member this.ConvertInputToModel = this.ConvertInputToModel
            

    type BoundAttachedProperty =
        { Name: string
          UniqueName: string
          DefaultValue: string
          InputType: string
          ModelType: string
          ConvertInputToModel: string
          ConvertModelToValue: string
          UpdateCode: string }
        interface IBoundMember with
            member this.UniqueName = this.UniqueName
            member this.InputType = this.InputType
            member this.ConvertInputToModel = this.ConvertInputToModel
    
    type BoundPropertyCollectionData =
        { ElementType: string
          AttachedProperties: BoundAttachedProperty array }
    
    type BoundProperty =
        { Name: string
          ShortName: string
          UniqueName: string
          DefaultValue: string
          OriginalType: string
          InputType: string
          ModelType: string
          ConvertInputToModel: string
          ConvertModelToValue: string
          UpdateCode: string
          CollectionData: BoundPropertyCollectionData option
          IsInherited: bool }
        interface IBoundConstructorMember with
            member this.ShortName = this.ShortName
            member this.UniqueName = this.UniqueName
            member this.InputType = this.InputType
            member this.ConvertInputToModel = this.ConvertInputToModel
            member this.IsInherited = this.IsInherited
        interface IBoundMember with
            member this.UniqueName = this.UniqueName
            member this.InputType = this.InputType
            member this.ConvertInputToModel = this.ConvertInputToModel
    
    type BoundType =
        { Type: string
          CanBeInstantiated: bool
          TypeToInstantiate: string
          BaseTypeName: string option
          Name: string
          Events: BoundEvent array
          Properties: BoundProperty array }
    
    type BoundModel =
        { Assemblies: string array
          OutputNamespace: string
          Types: BoundType array }