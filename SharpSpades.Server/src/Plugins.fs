// Copyright (c) JStalnac 2025
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades.Server

open System
open System.IO
open System.Reflection
open SharpSpades
open SharpSpades.Configuration

module Plugins =
    type PluginMetadata = {
        Id : string
        Name : string option
        Description : string option
        Version : string option
        Authors : string list option
        Url : string option
    }

    type AvailablePlugin = {
        Id : string
        AssemblyName : string
        Metadata : PluginMetadata
    }

    type SupervisorPlugin = {
        Init : ISupervisor -> Async<PluginBuilder>
    }

    type WorldPlugin = {
        Init : IWorld -> Async<PluginBuilder>
    }

    type PluginDescriptor = {
        Type : Type
        Metadata : PluginMetadata
        ConfigureServices : (IServiceCollection -> unit) option
        Supervisor : SupervisorPlugin option
        World : WorldPlugin option
    }

    let readPluginsToml () =
        async {
            let file =
                Assembly.GetExecutingAssembly().Location
                |> IO.getDirectoryName
                |> fun dir ->
                    Path.Combine(dir, "plugins.toml")
            let! res = IO.readFile file
            match res with
            | Ok contents ->
                let res =
                    Config.load contents
                    |> Result.bind
                        (fun config ->
                        (Ok [], config)
                        ||> Map.fold (fun state id value ->
                            state
                            |> Result.bind (fun plugins ->
                                match value with
                                | Section s ->
                                    // TODO: Add helper functions to
                                    // SharpSpades.Configuration to aid with this
                                    // kind of conversion code
                                    Config.getString "assembly_name" s
                                    |> Result.bind (fun opt ->
                                        match opt with
                                        | Some an -> Ok an
                                        | None -> Error "missing key 'assembly_name'")
                                    |> Result.bind (fun state ->
                                        Config.getString "name" s
                                        |> Result.map (fun n -> state, n))
                                    |> Result.bind (fun state ->
                                        Config.getString "description" s
                                        |> Result.map (fun d -> state, d))
                                    |> Result.bind (fun state ->
                                        Config.getString "version" s
                                        |> Result.map (fun v -> state, v))
                                    |> Result.bind (fun state ->
                                        Config.getList "authors" s
                                        |> Result.bind (fun opt ->
                                            match opt with
                                            | Some list ->
                                                list
                                                |> Config.castList (function
                                                    | String str -> Ok str
                                                    | _ -> Error "'authors' must be a string array")
                                                |> Result.map (fun list -> Some list)
                                            | None -> Ok None)
                                        |> Result.map (fun authors -> state, authors))
                                    |> Result.bind (fun state ->
                                        Config.getString "url" s
                                        |> Result.map (fun u -> state, u))
                                    |> Result.bind (fun (((((assemblyName, name), description), version), authors), url) ->
                                        Ok (
                                            { Id = id;
                                              AssemblyName = assemblyName;
                                              Metadata =
                                                { Id = id;
                                                  Name = name;
                                                  Description = description;
                                                  Version = version;
                                                  Authors = authors;
                                                  Url = url } : PluginMetadata
                                            } :: plugins))
                                | _ -> Error "expected table")))
                return res
            | Error err ->
                return Error (sprintf "%A" err)
        }

    let private getStaticMethod (ty : Type) name =
        let mi = ty.GetMethod(name, BindingFlags.Public ||| BindingFlags.Static)
        match mi with
        | null -> None
        | mi -> Some mi

    let private getConfigureServices (ty : Type) : Result<(IServiceCollection -> unit) option, string> =
        match getStaticMethod ty "configureServices" with
        | Some mi ->
            match (mi.GetParameters(), mi.ReturnType) with
            | [| p1 |], ret
                when p1.ParameterType = typeof<IServiceCollection>
                  && (ret = typeof<unit> && ret = typeof<IServiceCollection>)
                -> Ok (Some (fun services -> mi.Invoke(null, [| services |]) |> ignore))
            | _ -> Error "The type of configureServices must be either IServiceCollection -> IServiceCollection or IServiceCollection -> unit"
        | None -> Ok None

    let private getInitFunction<'T> (ty : Type) name : Result<('T -> Async<PluginBuilder>) option, string> =
        match getStaticMethod ty name with
        | Some mi ->
            match (mi.GetParameters(), mi.ReturnType) with
            | [| p1 |], ret when p1.ParameterType = typeof<'T> && ret = typeof<Async<PluginBuilder>>
                -> Ok (Some (fun w -> mi.Invoke(null, [| w |]) :?> Async<PluginBuilder>))
            | [| p1 |], ret when p1.ParameterType = typeof<'T> && ret = typeof<PluginBuilder>
                -> Ok (Some (fun w ->
                    async {
                        return mi.Invoke(null, [| w |]) :?> PluginBuilder
                    }))
            | _ ->
                let tyName = typeof<'T>.Name
                let pluginBuilder = nameof PluginBuilder
                Error $"The signature of {name} must be either {tyName} -> {pluginBuilder} or {tyName} -> Async<{pluginBuilder}>"
        | None -> Ok None

    let private makeDescriptor (meta : PluginMetadata) (ty : Type) : Result<PluginDescriptor, string list> =
        let configureServices = getConfigureServices ty
        let supervisorPlugin =
            getInitFunction<ISupervisor> ty "Supervisor"
            |> Result.map (Option.map (fun init -> { Init = init } : SupervisorPlugin))
        let worldPlugin =
            getInitFunction<IWorld> ty "World"
            |> Result.map (Option.map (fun init -> { Init = init }))

        match (configureServices, worldPlugin, supervisorPlugin) with
        | _, Ok None, Ok None -> Error [ "Missing initWorld and/or initSupervisor function" ]
        | Ok configureServices, Ok worldPlugin, Ok supervisorPlugin ->
            Ok { Type = ty; Metadata = meta; ConfigureServices = configureServices;
                 Supervisor = supervisorPlugin; World = worldPlugin; }
        | Error err1, Ok _, Ok _ -> Error [ err1 ]
        | Ok _, Error err2, Ok _ -> Error [ err2 ]
        | Ok _, Ok _, Error err3 -> Error [ err3 ]
        | Error err1, Error err2, Ok _ -> Error [ err1; err2 ]
        | Error err1, Ok _, Error err3 -> Error [ err1; err3 ]
        | Ok _, Error err2, Error err3 -> Error [ err2; err3 ]
        | Error err1, Error err2, Error err3 -> Error [err1; err2; err3]

    let loadPlugins (availablePlugins : AvailablePlugin list) =
        availablePlugins
        |> Array.ofList
        |> Array.map (fun p -> p, Assembly.Load(AssemblyName(p.AssemblyName)))
        |> Array.map (fun (plugin, assembly) ->
            let types =
                assembly.GetTypes()
                |> Array.filter (fun ty ->
                    ty.GetCustomAttributes()
                    |> Seq.exists (fun attr -> attr :? PluginMainAttribute))
            plugin, types)
        |> Array.map (fun (plugin, types) ->
            match types with
            | [| ty |] -> Ok (plugin, ty)
            | [||] ->
                Error (plugin.Id, [ sprintf "No plugin type" ])
            | types ->
                types
                |> Array.map (fun ty -> ty.FullName)
                |> fun ns -> String.Join(", ", ns)
                |> sprintf "Multiple plugin types: %s"
                |> fun msg -> Error (plugin.Id, [ msg ]))
        |> Array.map (Result.bind (fun (plugin, ty) ->
            makeDescriptor plugin.Metadata ty
            |> Result.mapError (fun errors -> plugin.Id, errors)))
        |> Array.fold (fun state res ->
            match state, res with
            | Ok plugins, Ok plugin -> Ok (plugin :: plugins)
            | Ok _, Error err -> Error [ err ]
            | Error errors, Error err -> Error (err :: errors)
            | Error errors, Ok _ -> Error errors
        ) (Ok [])

    let configureServices plugins services =
        plugins
        |> List.iter (fun plugin ->
            match plugin.ConfigureServices with
            | Some configureServices -> configureServices services
            | None -> ())
        services
