// Copyright (c) JStalnac 2025
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades.Server

open System.Threading
open System.Threading.Channels
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Serilog
open SharpSpades
open SharpSpades.Server.Plugins

type WorldOptions = {
        Id : WorldId
        Messages : Channel<WorldMessage>
        Output : ChannelWriter<SupervisorMessage>
        CancellationToken : CancellationToken
        Plugins : PluginDescriptor list
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

            while not opts.CancellationToken.IsCancellationRequested do
                let hasMsg, msg = input.TryRead()
                if hasMsg then
                    match msg with
                    | Stop ->
                        sendSupervisor (WorldStopped opts.Id)
                        return ()
                do! Async.Sleep 50

            sendSupervisor (WorldStopped opts.Id)
            return ()
        }

    interface IWorld with
        member _.Id: WorldId = opts.Id
        member _.Logger = logger
        member _.LoggerFactory = loggerFactory
        member _.ServiceProvider = services
