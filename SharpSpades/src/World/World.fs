// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades

open System.Collections.Generic
open System.Threading
open System.Threading.Channels
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Serilog
open SharpSpades
open SharpSpades.Plugins
open SharpSpades.World

type WorldOptions = {
    Id : WorldId
    Messages : Channel<WorldMessage>
    Output : ChannelWriter<SupervisorMessage>
    CancellationToken : CancellationToken
    Plugins : PluginDescriptor list
}

type WorldClient = {
    Id : ClientId
}

type World(scope : IServiceScope, opts : WorldOptions) as this =
    let services = scope.ServiceProvider

    do
        let guard = services.GetRequiredService<ScopeGuard>()
        match guard.Type with
        | None -> guard.Type <- Some (ScopeType.World this)
        | Some _ -> invalidOp "The service scope is already assigned to a supervisor or world"

    let loggerFactory = LoggerFactory.Create(fun c ->
        let logger =
            LoggerConfiguration()
                .Enrich.WithProperty("SpadesWorld", opts.Id)
                .WriteTo.Logger(Log.Logger)
                .CreateLogger()
        c.AddSerilog(logger, dispose = true) |> ignore)

    let logger = loggerFactory.CreateLogger("World")

    let eventManager = EventManager(logger)

    let clients = List<WorldClient>()

    let sendSupervisor msg =
        opts.Output.TryWrite(msg) |> ignore

    let mutable running = false

    member _.Messages = opts.Messages

    member _.Run() =
        async {
            if running then
                invalidOp "The world is already running"
            running <- true

            let input = this.Messages.Reader

            logger.LogInformation("Starting world {Id}", opts.Id)
            sendSupervisor (WorldStarting opts.Id)

            logger.LogInformation("Initialising plugins")
            let mutable pluginContainers = []
            let! res = Plugins.initWorld this eventManager opts.Plugins
            match res with
            | Ok p ->
                pluginContainers <- p
            | Error errors ->
                logger.LogError("Some plugins failed to load")
                for desc, errors in errors do
                    for (reason, ex) in errors do
                        match ex with
                        | Some ex ->
                            logger.LogError(ex, "Failed to load plugin {Plugin}: {Reason}",
                                desc.Metadata.Id, reason)
                        | None ->
                            logger.LogError("Failed to load plugin {Plugin}: {Reason}",
                                desc.Metadata.Id, reason)
                return ()
            logger.LogDebug("Plugins initialised")

            while not opts.CancellationToken.IsCancellationRequested do
                let hasMsg, msg = input.TryRead()
                if hasMsg then
                    match msg with
                    | Stop ->
                        sendSupervisor (WorldStopped opts.Id)
                        return ()
                    | TransferClient clientId ->
                        let client = { Id = clientId }
                        clients.Add(client)
                        ()
                    | PacketReceived (clientId, packet) ->
                        ()
                do! Async.Sleep 50

            sendSupervisor (WorldStopped opts.Id)
            return ()
        }

    interface IWorld with
        member _.Id: WorldId = opts.Id
        member _.Logger = logger
        member _.LoggerFactory = loggerFactory
        member _.ServiceProvider = services

        // This get JIT'd for each event type
        member _.FireEvent<'T when 'T :> IEvent>(ev : 'T) : unit =
            eventManager.Fire(ev)
