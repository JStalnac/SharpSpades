// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades.Server

open System
open System.Threading
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Serilog
open Serilog.Configuration
open Argu
open SharpSpades
open SharpSpades.Configuration
open SharpSpades.Server.Plugins

module Program =

    let configureServices (services : IServiceCollection) =
        services
            .AddScoped<ScopeGuard>(fun _ -> { Type = None })
            .AddScoped<IWorld>(fun s ->
                let guard = s.GetRequiredService<ScopeGuard>()
                match guard.Type with
                | Some (ScopeType.World w) -> w
                | Some _ -> invalidOp "Cannot access IWorld in supervisor"
                | None -> invalidOp "World not yet initialised"
            ).AddScoped<ISupervisor>(fun s ->
                let guard = s.GetRequiredService<ScopeGuard>()
                match guard.Type with
                | Some (ScopeType.Supervisor ins) -> ins
                | Some _ -> invalidOp "Cannot access ISupervisor in world"
                | None -> invalidOp "Supervisor not yet initialised"
            ).AddLogging(fun c ->
                c.AddSerilog() |> ignore)

    [<RequireQualifiedAccess>]
    type MainArguments =
        | [<Unique; AltCommandLineAttribute("-c")>] Config of config : string
        | [<SubCommand; CliPrefix(CliPrefix.None)>] Check

        interface IArgParserTemplate with
            member x.Usage =
                match x with
                | Config _ -> "Config file"
                | Check ->
                    "Check that the server is able to load by starting and stopping it"

    [<EntryPoint>]
    let main args =
        let parser = ArgumentParser.Create<MainArguments>(programName = "SharpSpades.Server")
        let results =
            try
                parser.ParseCommandLine(inputs = args, raiseOnUsage = true)
            with
                :? ArguParseException as e ->
                    eprintfn "%s" e.Message
                    exit 1

        let configFile =
            match results.TryGetResult(MainArguments.Config) with
            | Some file ->
                if not (IO.fileExists file) then
                    eprintfn "Config file %s not found" file
                    exit 1
                Some file
            | None ->
                if IO.fileExists "config.toml" then
                    Some "config.toml"
                else
                    None

        let config =
            match configFile with
            | Some file ->
                IO.readFile file
                |> Async.RunSynchronously
                |> function
                    | Ok contents ->
                        Config.load contents
                        |> function
                            | Ok config -> config
                            | Error err ->
                                eprintfn "Failed to load config: %s" err
                                exit 1
                    | Error ioError ->
                        eprintfn "Failed to load config: %A" ioError
                        exit 1
            | None ->
                Map []

        let writeToConsole outputTemplate (wt : LoggerSinkConfiguration) =
            wt.Console(outputTemplate = outputTemplate) |> ignore

        Log.Logger <- LoggerConfiguration()
            .WriteTo.Conditional(
                (fun ev -> ev.Properties.ContainsKey("SpadesWorld")),
                writeToConsole "[{Timestamp:HH:mm:ss} {Level:u3}] [world/{SpadesWorld}:{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Conditional(
                (fun ev -> ev.Properties.ContainsKey("SpadesSupervisor")),
                writeToConsole "[{Timestamp:HH:mm:ss} {Level:u3}] [sup:{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Conditional(
                (fun ev ->
                    not (ev.Properties.ContainsKey("SpadesWorld") || ev.Properties.ContainsKey("SpadesSupervisor"))),
                writeToConsole "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger()

        let logger =
            LoggerFactory.Create(fun c -> c.AddSerilog() |> ignore)
                .CreateLogger("Main")

        logger.LogInformation("Starting SharpSpades")

        logger.LogInformation("Loading plugins")
        let plugins =
            Plugins.readPluginsToml ()
            |> Async.RunSynchronously
            |> function
                | Error err ->
                    eprintfn "Failed to read plugins.toml: %s" err
                    exit 1
                | Ok available ->
                    match Plugins.loadPlugins available with
                    | Ok plugins -> plugins
                    | Error errors ->
                        logger.LogCritical("Failed to load plugins due to errors in plugins")
                        for (plugin, errors) in errors do
                            errors
                            |> List.map (fun e -> sprintf "\n - %s" e)
                            |> String.concat ""
                            |> fun msg -> sprintf "Errors of plugin {Plugin}:%s" msg
                            |> fun msg ->
                                logger.LogCritical(msg, plugin)
                        Log.CloseAndFlush()
                        exit 1
                        []
        logger.LogDebug("Loaded plugins: {Plugins}",
            plugins |> List.map (fun p -> p.Metadata.Id) |> List.toArray)

        logger.LogDebug("Initialising services")
        let services =
            ServiceCollection()
            |> configureServices
            |> Plugins.configureServices plugins
            |> fun services ->
                let opts = ServiceProviderOptions()
                opts.ValidateOnBuild <- true
                services.BuildServiceProvider(opts)

        use cts = new CancellationTokenSource()
        Console.CancelKeyPress.AddHandler(fun _ args ->
            if not cts.IsCancellationRequested then
                logger.LogInformation("Stopping, press Ctrl + C again for force stop")
                args.Cancel <- true
                cts.Cancel()
            else
                logger.LogWarning("Forcefully stopping server, some data may not be saved")
                args.Cancel <- false)

        try
            match Config.getInteger "port" config with
            | Ok port ->
                // TODO: Reload config when it changes
                use scope = services.CreateScope()
                let supervisor = Supervisor(scope, {
                    Port = port |> Option.map int
                    CancellationToken = cts.Token
                    Plugins = plugins
                })
                if results.Contains(MainArguments.Check) then
                    logger.LogInformation("Server started succesfully. Stopping...")
                else
                    supervisor.Run() |> Async.RunSynchronously
            | Error err ->
                logger.LogCritical("Config error: {Error}", err)
        with
            ex ->
                logger.LogCritical(ex, "The supervisor crashed")
                cts.Cancel()

        logger.LogInformation("Server stopped")

        Log.CloseAndFlush()

        0
