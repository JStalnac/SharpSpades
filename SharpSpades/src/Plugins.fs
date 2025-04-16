namespace SharpSpades

open System

[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
type PluginMainAttribute() =
    inherit Attribute()
