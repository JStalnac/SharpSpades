// Copyright (c) JStalnac 2025
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades.Server

open System
open System.Threading
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Serilog
open SharpSpades
open SharpSpades.Native.Net

type SupervisorOptions = {
        Port : int option
        CancellationToken : CancellationToken
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

    let mutable running = false

    member _.Run() =
        async {
            if running then
                invalidOp "The supervisor is already running"
            running <- true

            logger.LogInformation("Starting")

            let port = opts.Port |> Option.defaultValue 32887 |> uint16
            use host = NetHost.CreateListener(AddressType.Any, port, 32u, 1u)

            host.OnConnect (fun (client : ClientId) version -> CallbackResult.Continue)
            host.OnReceive (fun (client : ClientId) buffer -> CallbackResult.Continue)
            host.OnDisconnect (fun (client : ClientId) ty -> CallbackResult.Continue)

            logger.LogInformation("Listening on port {Port}", port)
            try
                while not opts.CancellationToken.IsCancellationRequested do
                    if host.PollEvents(TimeSpan.FromMilliseconds(50)) < 0 then
                        logger.LogWarning("Failed to poll network events")

                logger.LogInformation("Stopping")
            finally
                logger.LogDebug("Stopped")
        }

    interface ISupervisor with
        member _.ServiceProvider = services
        member _.Logger = logger
        member _.LoggerFactory = loggerFactory
