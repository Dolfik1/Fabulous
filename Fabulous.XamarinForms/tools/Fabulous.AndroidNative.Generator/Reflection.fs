// Copyright 2018-2019 Fabulous contributors. See LICENSE.md for license.
namespace Fabulous.AndroidNative.Generator

open System
open System.IO
open System.Runtime.Loader
open Fabulous.CodeGen
open Fabulous.CodeGen.AssemblyReader.Models
open Mono.Cecil

module Reflection =    
    let loadAllAssemblies (paths: seq<string>) =
        let toFullPath p = Path.Combine(Environment.CurrentDirectory, p)
        
        paths
        |> Seq.map (toFullPath >> AssemblyLoadContext.Default.LoadFromAssemblyPath)
        |> Seq.toArray
    
    let tryGetPropertyInAssembly (assembly: AssemblyDefinition) (typeName, propertyName) =
        let toCleanTypeName propertyReturnType =
            propertyReturnType.ToString()
                              .Replace("[", "<")
                              .Replace("]", ">")
            |> Text.removeDotNetGenericNotation
        
        let typ =
            assembly.Modules
            |> Seq.choose (fun x -> x.GetType(typeName) |> Option.ofObj)
            |> Seq.tryHead
            
        if typ.IsNone then
            None
        else
            let typ = typ.Value
            if typ.ContainsGenericParameter then
                None // Generic types are not supported
            else
                let propName = propertyName
                let propertyInfo = typ.Properties |> Seq.tryFind (fun x -> x.Name = propName)
                if propertyInfo.IsNone then
                    None
                else
                    let property = propertyInfo.Value
                    if property = null then
                        None
                    else
                        Some
                            { Name = propertyName
                              Type = property.PropertyType |> toCleanTypeName
                              DefaultValue = None } // DefaultValue here is not relevant since we will call ClearValue instead to reset the property
                    
                            
    let tryGetProperty (assemblies: AssemblyDefinition array) (typeName, propertyName) =
        assemblies
        |> Array.tryPick (fun asm -> tryGetPropertyInAssembly asm (typeName, propertyName))