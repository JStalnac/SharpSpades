#!/usr/bin/env -S dotnet fsi --
#r "nuget: Argu, 6.2.5"
#r "nuget: Samboy063.Tomlet, 6.0.0"

// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

open System
open System.IO
open System.Diagnostics
open Argu
open Tomlet
open Tomlet.Models
open Tomlet.Exceptions

type PluginId = string

type Plugin = {
        Id : PluginId
        AssemblyName : string
        ProjectFile : string
        Name : string option
        Description : string option
        Version : string option
        Authors : string array option
        Url : string option
    }

type EnabledPlugin =
    | Available of Plugin
    | Unavailable of PluginId * error : string

let isValidId (id : string) =
    id
    |> fun s -> s.Split('.')
    |> Array.forall (fun part ->
        if String.IsNullOrEmpty(part) then
            false
        else
            part
            |> Seq.mapi (fun i c -> i, c)
            |> Seq.forall (function
                | _, letter when Char.IsAsciiLetter(letter) -> true
                | n, digit when n > 0 && Char.IsAsciiDigit(digit) -> true
                | _, '_' -> true
                | _ -> false))

let combine p1 p2 = Path.Combine(p1, p2)
let combine3 p1 p2 p3 = Path.Combine(p1, p2, p3)

let rootDir = __SOURCE_DIRECTORY__
let serverDir = combine rootDir "SharpSpades.Server/src"
let pluginsDir = combine rootDir "plugins"

let runCommand cmdName (args : string) =
    async {
        printfn "%s %s" cmdName args
        let startInfo = ProcessStartInfo(cmdName, args,
                UseShellExecute = false,
                CreateNoWindow = true
            )
        use p = Process.Start(startInfo)
        do! p.WaitForExitAsync() |> Async.AwaitTask
        return
            match p.ExitCode with
            | 0 -> Ok ()
            | other -> Error other
    }

let dotnet = runCommand "dotnet"

let readFile (path : string) =
    async {
        try
            let! contents =
                File.ReadAllTextAsync(path)
                |> Async.AwaitTask
            return Ok contents
        with
            | :? FileNotFoundException ->
                return Error (sprintf "%s: file not found"
                    (Path.GetRelativePath(rootDir, path)))
            | :? DirectoryNotFoundException ->
                return Error (sprintf "%s: directory not found"
                    (Path.GetRelativePath(rootDir, path)))
            | :? IOException as e ->
                return Error (sprintf "%s: IO error: %s"
                    (Path.GetRelativePath(rootDir, path)) e.Message)
    }

let writeFile (path : string) (contents : string) =
    async {
        try
            do!
                File.WriteAllTextAsync(path, contents)
                |> Async.AwaitTask
            return Ok ()
        with
            | :? FileNotFoundException ->
                return Error (sprintf "%s: file not found"
                    (Path.GetRelativePath(rootDir, path)))
            | :? DirectoryNotFoundException ->
                return Error (sprintf "%s: directory not found"
                    (Path.GetRelativePath(rootDir, path)))
            | :? IOException as e ->
                return Error (sprintf "%s: IO error: %s"
                    (Path.GetRelativePath(rootDir, path)) e.Message)
    }

let parseToml text =
    try
        TomlParser().Parse(text) |> Ok
    with
        :? TomlException as e ->
            Error (sprintf "%s: %s" (e.GetType().Name) e.Message)

let pluginExists id =
    Directory.EnumerateDirectories(pluginsDir)
    |> Seq.contains id

let getPlugin id =
    async {
        let dir = combine pluginsDir id
        let pluginToml = combine dir "plugin.toml"
        match (Directory.Exists(dir), File.Exists(pluginToml)) with
        | true, true ->
            let! contents = readFile pluginToml
            let makePlugin id assemblyName projectFile name
                    description version authors url
                =
                {
                    Id = id;
                    AssemblyName = assemblyName;
                    ProjectFile = projectFile;
                    Name = name;
                    Description = description;
                    Version = version;
                    Authors = authors;
                    Url = url;
                }

            return
                contents
                |> Result.bind parseToml
                |> Result.bind (fun doc ->
                    let (plugin, errors) =
                        let getRequired key =
                            if doc.ContainsKey(key) then
                                Ok (doc.GetValue(key))
                            else
                                Error (sprintf "missing %s" key)
                        let getRequiredString key =
                            getRequired key
                            |> Result.bind (fun value ->
                                match value with
                                | :? TomlString as str -> Ok str.Value
                                | _ -> Error (sprintf "%s must be string" key))

                        let getOptional key =
                            if doc.ContainsKey(key) then
                                Some (doc.GetValue(key))
                            else
                                None
                        let getOptionalString key =
                            match getOptional key with
                            | Some value ->
                                match value with
                                | :? TomlString as str -> Ok (Some str.Value)
                                | _ -> Error (sprintf "%s must be string" key)
                            | None -> Ok None

                        let propagate make errors =
                            function
                            | Ok x -> make x, errors
                            | Error err -> make (Unchecked.defaultof<_>), err :: errors

                        (makePlugin id, [])
                        |> fun (make, errors) ->
                            getOptionalString "assembly_name"
                            |> Result.bind (function
                                | Some str ->
                                    match str with
                                    | an when isValidId an ->
                                        Ok an
                                    | an ->
                                        Error (sprintf "'%s' is an invalid assembly name" an)
                                | None -> Ok id)
                                |> propagate make errors
                        |> fun (make, errors) ->
                            getRequiredString "project"
                            |> Result.bind (function
                                | s when String.IsNullOrWhiteSpace(s)
                                      || s = String.Empty
                                    -> Error "invalid project file path"
                                | s -> Ok s)
                            |> propagate make errors
                        |> fun (make, errors) ->
                            getOptionalString "name"
                            |> propagate make errors
                        |> fun (make, errors) ->
                            getOptionalString "description"
                            |> propagate make errors
                        |> fun (make, errors) ->
                            getOptionalString "version"
                            |> propagate make errors
                        |> fun (make, errors) ->
                            getOptional "authors"
                            |> function
                                | Some authors ->
                                    match authors with
                                    | :? TomlArray as array ->
                                        array.ArrayValues
                                        |> Seq.forall (fun v -> v :? TomlString)
                                        |> function
                                            | true ->
                                                array.ArrayValues
                                                |> Seq.map (fun str -> (str :?> TomlString).Value)
                                                |> Array.ofSeq
                                                |> Some
                                                |> Ok
                                            | false -> Error "all values in authors must be string"
                                    | :? TomlString as str ->
                                        Ok (Some [| str.Value |])
                                    | _ -> Error "authors must be string or string array"
                                | None -> Ok None
                            |> propagate make errors
                        |> fun (make, errors) ->
                            getOptionalString "url"
                            |> propagate make errors

                    match errors with
                    | [] -> Ok plugin
                    | errors ->
                        String.Join(", ", errors)
                        |> sprintf "Invalid plugin.toml: %s"
                        |> Error)
        | true, false -> return Error (sprintf "No plugin.toml for plugin %s" id)
        | false, false -> return Error (sprintf "Missing plugin %s" id)
        | _ -> return Error "unreachable code"
    }

let getEnabledPlugins () =
    async {
        let pluginsToml = combine serverDir "plugins.toml"
        if File.Exists(pluginsToml) then
            let! res = readFile pluginsToml
            let parsed =
                res
                |> Result.bind parseToml
                |> Result.map (fun doc ->
                    doc.Keys
                    |> Seq.map (fun id ->
                        async {
                            match! getPlugin id with
                            | Ok plugin -> return Available plugin
                            | Error err -> return Unavailable (id, err)
                        })
                    |> Async.Parallel)

            match parsed with
            | Ok enabled ->
                let! enabled = enabled
                return Ok (enabled |> List.ofArray)
            | Error err ->
                return Error (sprintf "Failed to parse plugins.toml: %s" err)
        else
            return Ok []
    }

let saveEnabledPlugins (plugins : EnabledPlugin list) =
    async {
        let table = TomlDocument.CreateEmpty()
        let plugins =
            plugins
            |> List.sortBy (function
                | Available x -> x.Id
                | Unavailable (x, _) -> x)
        for plugin in plugins do
            let p = TomlTable()
            p.ForceNoInline <- true
            match plugin with
            | Available plugin ->
                p.Put("assembly_name", plugin.AssemblyName)
                match plugin.Name with
                | Some name -> p.Put("name", name)
                | _ -> ()
                match plugin.Description with
                | Some desc -> p.Put("description", desc)
                | _ -> ()
                match plugin.Version with
                | Some version -> p.Put("version", version)
                | _ -> ()
                match plugin.Authors with
                | Some authors -> p.Put("authors", authors)
                | _ -> ()
                match plugin.Url with
                | Some url -> p.Put("url", url)
                | _ -> ()
                table.PutValue(plugin.Id, p)
            | Unavailable (id, _) ->
                table.PutValue(id, p)

        let! res =
            table.SerializedValue
            |> fun toml ->
                sprintf "%s\n\n%s"
                    "# Do not edit this file!"
                    toml
            |> writeFile (combine serverDir "plugins.toml")

        let available =
            plugins
            |> List.choose (function
                | Available p -> Some p
                | _ -> None)

        match res with
        | Error err -> return Error err
        | Ok _ ->
            let! res =
                String.concat "\n" [
                "<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">";
                "  <ItemGroup>";

                for plugin in available do
                    sprintf """    <ProjectReference Include="%s" Private="true" />"""
                        (combine3 "../../plugins" plugin.Id plugin.ProjectFile)

                "  </ItemGroup>";
                "</Project>\n";
                ]
                |> writeFile (combine serverDir "Plugins.targets")
            match res with
            | Ok _ -> return Ok ()
            | Error err ->
                return Error err
    }

let enablePlugins plugins =
    async {
        let plugins =
            plugins
            |> List.map Available
        match! getEnabledPlugins () with
        | Ok enabled ->
            let! res =
                plugins @ enabled
                |> saveEnabledPlugins
            return
                match res with
                | Ok _ -> Ok ()
                | Error err ->
                    Error (sprintf "Failed to enable plugins: %s" err)
        | Error err -> return Error (sprintf "Failed to read enabled plugins: %s" err)
    }

let disablePlugins toDisable =
    async {
        let contains plugins id =
            plugins
            |> List.exists ((=) id)

        match! getEnabledPlugins () with
        | Ok enabled ->
            let! res =
                enabled
                |> List.filter (not << function
                    | Available p -> contains toDisable p.Id
                    | Unavailable (id, _) -> contains toDisable id)
                |> saveEnabledPlugins
            match res with
            | Ok () -> return Ok ()
            | Error err -> return Error (sprintf "Failed to save plugins.toml: %s" err)
        | Error err -> return Error (sprintf "Failed to read plugins.toml: %s" err)
    }

type NewPluginOptions = {
        Name : string
        Restore : bool
        AddToSln : bool
        EnablePlugin : bool
    }

let newPluginCmd (id : string) (opts : NewPluginOptions) =
    async {
        if not (isValidId id) then
                eprintfn "'%s' is not a valid plugin ID" id
                exit 1

        if pluginExists id then
            eprintfn "Plugin with id %s already exists" id
            exit 1

        printfn "Creating project..."

        let pluginDir = combine pluginsDir id

        let _ = Directory.CreateDirectory(pluginDir)
        let! res =
            """<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Plugin.fs" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\SharpSpades\src\SharpSpades.fsproj" />
  </ItemGroup>

</Project>"""
            |> writeFile (combine pluginDir (sprintf "%s.fsproj" id))
        match res with
        | Ok _ -> ()
        | Error err ->
            eprintfn "Failed to create project file %s" err
            exit 1

        let! res =
            id |> sprintf """namespace %s

open SharpSpades

[<PluginMain>]
module Plugin =
    let initWorld (w : IWorld) =
        async {
            Plugin.newBuilder ()
        }
"""
            |> writeFile (combine pluginDir "Plugin.fs")
        match res with
        | Ok _ -> ()
        | Error err ->
            eprintfn "%s" err
            exit 1

        let! res =
            (opts.Name, sprintf "%s.fsproj" id) ||> sprintf """name = "%s"
project = "%s"
"""
            |> writeFile (combine pluginDir "plugin.toml")
        match res with
        | Ok () -> ()
        | Error err ->
            eprintfn "Failed to create plugin.toml: %s" err
            exit 1

        if opts.AddToSln then
            printfn "Adding project to SharpSpades.sln..."
            let! res =
                sprintf "sln %s add %s"
                    (combine rootDir "SharpSpades.sln")
                    (combine pluginsDir id)
                |> dotnet
            match res with
            | Ok _ -> ()
            | Error _ ->
                eprintfn "Failed to add project to solution file"
                exit 1

        if opts.Restore then
            printfn "Restoring project..."
            let! res =
                sprintf "restore %s"
                    (combine pluginsDir id)
                |> dotnet
            match res with
            | Ok _ -> ()
            | Error _ ->
                eprintfn "Restore failed"
                exit 1

        if opts.EnablePlugin then
            printfn "Enabling plugin..."
            let! res =
                enablePlugins [{
                        Id = id;
                        AssemblyName = id;
                        ProjectFile = sprintf "%s.fsproj" id;
                        Name = Some opts.Name;
                        Description = None;
                        Version = None;
                        Authors = None;
                        Url = None;
                    }]
            printfn "Result of enabling: %A" res
            ()

        printfn "Done!"

        ()
    }

type EnablePluginsOptions = {
        AddToSln : bool
    }

let enablePluginsCmd plugins (opts : EnablePluginsOptions) =
    async {
        let! plugins =
            plugins
            |> List.map (fun id ->
                async {
                    match! getPlugin id with
                    | Ok plugin -> return Ok plugin
                    | Error err -> return Error (id, err)
                })
            |> Async.Parallel

        let res =
            (Ok [], plugins)
            ||> Array.fold (fun state p ->
                match state, p with
                | Ok state, Ok p -> Ok (p :: state)
                | Error (), Ok _ -> Error ()
                | Ok _, Error (id, err)
                | Error (), Error (id, err) ->
                    eprintfn "Cannot enable plugin %s due to errors: %s" id err
                    Error ())

        if not (Result.isOk res) then
            eprintfn "Failed to enable plugins"
            exit 1

        let plugins = res |> Result.defaultValue []

        let! res = enablePlugins plugins
        match res with
        | Ok () ->
            printfn "Plugins enabled"
            if opts.AddToSln then
                printfn "Adding plugins to SharpSpades.sln"
                for plugin in plugins do
                    let! res =
                        sprintf "sln %s add %s"
                            (combine rootDir "SharpSpades.sln")
                            (combine3 pluginsDir plugin.Id plugin.ProjectFile)
                        |> dotnet
                    match res with
                    | Ok () -> ()
                    | Error _ ->
                        eprintfn "Failed to add %s to solution file" plugin.Id
                        exit 1
        | Error err ->
            eprintfn "Failed to enable plugins: %s" err
            exit 1
    }

type DisablePluginsOptions = {
        RemoveFromSln : bool
    }

let disablePluginsCmd plugins (opts : DisablePluginsOptions) =
    async {
        let! res = disablePlugins plugins
        match res with
        | Ok () -> ()
        | Error err ->
            eprintfn "Failed to disable plugins: %s" err
            exit 1

        printfn "Removed plugins"

        if not opts.RemoveFromSln then
            return ()

        printfn "Removing plugins from SharpSpades.sln"

        for id in plugins do
            if pluginExists id then
                match! getPlugin id with
                | Ok plugin ->
                    printfn "Removing %s" plugin.Id
                    let! res =
                        sprintf "sln %s remove %s"
                            (combine rootDir "SharpSpades.sln")
                            (combine3 pluginsDir plugin.Id plugin.ProjectFile)
                        |> dotnet
                    match res with
                    | Ok () -> ()
                    | Error n ->
                        eprintfn "Failed to remove %s from solution file" plugin.Id
                | Error err ->
                    eprintfn "Cannot remove %s due to error in plugin.toml: %s"
                        id err
    }

let listPluginsCmd onlyEnabled =
    async {
        if onlyEnabled then
            let! res = getEnabledPlugins ()
            match res with
            | Ok enabled ->
                printfn "Enabled plugins:"
                for plugin in enabled do
                    match plugin with
                    | Available plugin ->
                        printfn "%s" plugin.Id
                    | Unavailable (id, err) ->
                        printfn "%s (Unavailable due to error: %s)" id err
            | Error err ->
                eprintfn "Failed to read plugins.toml: %s" err
        else
            printfn "Available plugins:"
            for dir in Directory.EnumerateDirectories(pluginsDir) do
                let id = Path.GetFileName(dir)
                printfn "%s" id
    }

[<RequireQualifiedAccess>]
type NewArgs =
    | [<ExactlyOnce; MainCommand>] Id of id : string
    | [<Unique; AltCommandLine("-n")>] Name of name : string
    | [<CustomCommandLine("--no-restore")>] NoRestore
    | Enable
    | [<CustomCommandLine("--no-sln")>] NoSln

    interface IArgParserTemplate with
        member x.Usage =
            match x with
            | Id _ -> "Identifier for the plugin"
            | Name _ -> "Display name for the plugin"
            | NoRestore -> "Do not run dotnet restore in the created project"
            | Enable -> "Enable the plugin in the server after creating"
            | NoSln -> "Do not add the new plugin to the SharpSpades.sln solution file (not recommended)"

[<RequireQualifiedAccess>]
type EnableArgs =
    | [<Mandatory; MainCommand>] Plugins of plugin : string list
    | [<CustomCommandLine("--no-sln")>] NoSln

    interface IArgParserTemplate with
        member x.Usage =
            match x with
            | Plugins _ -> "The list of plugins to enable"
            | NoSln -> "Do not add the plugins to the SharpSpades.sln solution file (not recommended)"

[<RequireQualifiedAccess>]
type DisableArgs =
    | [<Mandatory; MainCommand>] Plugins of plugin : string list
    | [<CustomCommandLine("--no-sln")>] NoSln

    interface IArgParserTemplate with
        member x.Usage =
            match x with
            | Plugins _ -> "The list of plugins to disable"
            | NoSln -> "Do not remove the plugins from the SharpSpades.sln solution file"

[<RequireQualifiedAccess>]
type ListArgs =
    | Available

    interface IArgParserTemplate with
        member x.Usage =
            match x with
            | Available -> "List available plugins"


[<RequireSubcommand; RequireQualifiedAccess>]
type MainArguments =
    | [<SubCommand; CliPrefix(CliPrefix.None)>] New of ParseResults<NewArgs>
    | [<SubCommand; CliPrefix(CliPrefix.None)>] Enable of ParseResults<EnableArgs>
    | [<SubCommand; CliPrefix(CliPrefix.None)>] Disable of ParseResults<DisableArgs>
    | [<SubCommand; CliPrefix(CliPrefix.None)>] List of ParseResults<ListArgs>
    | [<SubCommand; CliPrefix(CliPrefix.None)>] Update

    interface IArgParserTemplate with
        member x.Usage =
            match x with
            | New _ -> "Create a new plugin from a template"
            | Enable _ -> "Enable plugins in the server"
            | Disable _ -> "Disable plugins in the server"
            | List _ -> "List plugins"
            | Update -> "Update plugins.toml with plugin metadata from plugin.toml files"


let (|New|Enable|Disable|List|Update|) (results : ParseResults<MainArguments>) =
    match results with
    | results when results.Contains(MainArguments.New)
        -> New (results.GetResult(MainArguments.New))
    | results when results.Contains(MainArguments.Enable)
        -> Enable (results.GetResult(MainArguments.Enable))
    | results when results.Contains(MainArguments.Disable)
        -> Disable (results.GetResult(MainArguments.Disable))
    | results when results.Contains(MainArguments.List)
        -> List (results.GetResult(MainArguments.List))
    | results when results.Contains(MainArguments.Update)
        -> Update
    | _ -> failwith "Unknown command (script is broken)"

let parser = ArgumentParser.Create<MainArguments>(programName = fsi.CommandLineArgs[0])
let results =
    try
        parser.ParseCommandLine(inputs = (fsi.CommandLineArgs |> Array.tail),
            raiseOnUsage = true)
    with :? ArguParseException as e ->
        eprintfn "%s" e.Message
        exit 1

if not (Directory.Exists(pluginsDir)) then
    Directory.CreateDirectory(pluginsDir) |> ignore

match results with
| New args ->
    let id = args.GetResult(NewArgs.Id)
    newPluginCmd id {
        Name = args.GetResult(NewArgs.Name, id)
        Restore = not (args.Contains(NewArgs.NoRestore))
        AddToSln = not (args.Contains(NewArgs.NoSln))
        EnablePlugin = args.Contains(NewArgs.Enable)
    }
     |> Async.RunSynchronously
| Enable args ->
    let plugins = args.GetResult(EnableArgs.Plugins)
    enablePluginsCmd plugins {
        AddToSln = not (args.Contains(EnableArgs.NoSln))
    }
    |> Async.RunSynchronously
| Disable args ->
    let plugins = args.GetResult(DisableArgs.Plugins)
    disablePluginsCmd plugins {
        RemoveFromSln = not (args.Contains(DisableArgs.NoSln))
    } |> Async.RunSynchronously
| List args ->
    listPluginsCmd (not (args.Contains(ListArgs.Available)))
    |> Async.RunSynchronously
| Update ->
    async {
        printfn "Updating plugins.toml with new metadata from plugins..."
        let! res = getEnabledPlugins ()
        match res with
        | Ok plugins ->
            match! saveEnabledPlugins plugins with
            | Ok () -> printfn "Done!"
            | Error err -> eprintfn "Error: %s" err
        | Error err -> eprintfn "Error: %s" err
    } |> Async.RunSynchronously
