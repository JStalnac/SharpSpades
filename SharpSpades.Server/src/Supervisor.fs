// Copyright (c) JStalnac 2025
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades.Server

open System
open System.Threading
open System.Threading.Channels
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Serilog
open SharpSpades
open SharpSpades.Native.Net
open SharpSpades.Server.Plugins

type SupervisorOptions = {
        Port : int option
        CancellationToken : CancellationToken
        Plugins : PluginDescriptor list
    }

type Supervisor(scope : IServiceScope, opts : SupervisorOptions) as this =
    let services = scope.ServiceProvider

    do
        let guard = services.GetRequiredService<ScopeGuard>()
        match guard.Type with
        | None -> guard.Type <- Some (ScopeType.Supervisor this)
        | Some _ -> invalidOp "The service scope is already assigned to a supervisor or world"

    let loggerFactory = LoggerFactory.Create(fun c ->
        let logger =
            LoggerConfiguration()
                .Enrich.WithProperty("SpadesSupervisor", "supervisor")
                .WriteTo.Logger(Log.Logger)
                .CreateLogger()
        c.AddSerilog(logger, dispose = true) |> ignore)

    let logger = loggerFactory.CreateLogger("Supervisor")

    let eventManager = EventManager(logger)

    let mutable running = false

    member _.Run() =
        async {
            if running then
                invalidOp "The supervisor is already running"
            running <- true

            logger.LogInformation("Starting")

            let input = Channel.CreateUnbounded<SupervisorMessage>()

            logger.LogInformation("Initialising plugins")
            let! _ =
                opts.Plugins
                |> List.choose (fun descriptor ->
                    match descriptor.Supervisor with
                    | Some plugin ->
                        // TODO: Catch exceptions
                        plugin.Init this |> Some
                    | None -> None)
                |> Async.Parallel
            logger.LogDebug("Plugins initialised")

            let world = World(services.CreateScope(), {
                    Id = "main"
                    Messages = Channel.CreateUnbounded()
                    Output = input.Writer
                    CancellationToken = opts.CancellationToken
                    Plugins = opts.Plugins
                })
            world.Run() |> Async.Start

            let port = opts.Port |> Option.defaultValue 32887 |> uint16
            use host = NetHost.CreateListener(AddressType.IPv4, port, 32u, 1u)

            host.OnConnect (fun (client : ClientId) version ->
                CallbackResult.Continue)
            host.OnReceive (fun (client : ClientId) buffer ->
                CallbackResult.Continue)
            host.OnDisconnect (fun (client : ClientId) ty ->
                CallbackResult.Continue)

            logger.LogInformation("Listening on port {Port}", port)
            try
                while not opts.CancellationToken.IsCancellationRequested do
                    if host.PollEvents(TimeSpan.FromMilliseconds(50)) < 0 then
                        logger.LogWarning("Failed to poll network events")

                    let mutable messagesRead = 0
                    while messagesRead < 50 && input.Reader.Count > 0 do
                        let hasMsg, msg = input.Reader.TryRead()
                        if hasMsg then
                            match msg with
                            | WorldStarting(worldId) ->
                                logger.LogInformation("World {WorldId} starting", worldId)
                                world.Messages.Writer.TryWrite(Stop) |> ignore
                            | WorldStopping(_) -> failwith "Not Implemented"
                            | WorldStopped(worldId) ->
                                logger.LogInformation("World {WorldId} stopped", worldId)
                        messagesRead <- messagesRead + 1

                logger.LogInformation("Stopping")
            finally
                logger.LogDebug("Stopped")
        }

    interface ISupervisor with
        member _.ServiceProvider = services
        member _.Logger = logger
        member _.LoggerFactory = loggerFactory

        // This gets JIT'd for each event type
        member _.FireEvent<'T when 'T :> Event>(ev : 'T) : unit =
            eventManager.Fire(ev)
