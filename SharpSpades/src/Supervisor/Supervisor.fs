// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades.Supervisor

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Channels
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Serilog
open SharpSpades
open SharpSpades.Supervisor
open SharpSpades.World
open SharpSpades.Net
open SharpSpades.Native.Net
open SharpSpades.Plugins

type SupervisorOptions = {
    Port : int option
    CancellationToken : CancellationToken
    Plugins : PluginDescriptor list
}

type ClientInfo = {
    ProtocolVersion : ProtocolVersion
    mutable Stats : ClientStats
    mutable World : WorldId option
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

    let port = opts.Port |> Option.defaultValue 32887 |> uint16
    let host = NetHost.CreateListener(AddressType.IPv4, port, 32u, 1u)

    let eventManager = EventManager(logger)
    do
        eventManager.RegisterEvent<OnClientConnect>() |> ignore
        ()

    let messages = Channel.CreateUnbounded<SupervisorMessage>()
    let clients = Dictionary<ClientId, ClientInfo>()

    let tryFindClient id =
        match clients.TryGetValue(id) with
        | true, client -> Some client
        | false, _ -> None

    let sendPacket client flags (packet : Packet) =
        match tryFindClient client with
        | Some _ ->
            match host.SendPacket(client, flags, packet.Buffer) with
            | 0 -> Ok ()
            | _ -> Error "Failed to send packet"
        | None ->
            Error "Client not found"

    let handleConnect clientId version =
        let ev = { Version = version }
        Supervisor.fireEvent this ev
        clients.Add(clientId, {
            ProtocolVersion = version
            Stats = {
                Address = System.Net.IPEndPoint(System.Net.IPAddress.Any, 32887)
                IncomingBandwidth = 0u
                OutgoingBandwidth = 0u
                PacketLoss = 0u
                RoundTripTime = 0u
            }
            World = None
        })
        logger.LogInformation("Client {ClientId} connected", clientId)
        ()

    let handleDisconnect clientId disconnectType =
        let ev = { Reason = disconnectType } : OnClientDisconnect
        Supervisor.fireEvent this ev
        let removed = clients.Remove(clientId)
        if not removed then
            logger.LogWarning("Client {ClientId} disconnected but was not tracked in state", clientId)
        logger.LogInformation("Client {ClientId} disconnected", clientId)
        ()

    let mutable running = false

    member _.Run() =
        async {
            if running then
                invalidOp "The supervisor is already running"
            running <- true

            logger.LogInformation("Starting")

            logger.LogInformation("Initialising plugins")
            let mutable pluginContainers = []
            let! res = Plugins.initSupervisor this eventManager opts.Plugins
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

            let world = World(services.CreateScope(), {
                    Id = "main"
                    Messages = Channel.CreateUnbounded()
                    Output = messages.Writer
                    CancellationToken = opts.CancellationToken
                    Plugins = opts.Plugins
                })
            world.Run() |> Async.Start

            host.OnConnect (fun (clientId : ClientId) version ->
                handleConnect clientId version
                let client = tryFindClient clientId |> Option.get
                client.World <- Some "main"
                logger.LogInformation("Transferring client {ClientId} to world {World}", clientId, "main")
                world.Messages.Writer.TryWrite(TransferClient clientId) |> ignore
                CallbackResult.Continue)
            host.OnReceive (fun (client : ClientId) buffer ->
                CallbackResult.Continue)
            host.OnDisconnect (fun (client : ClientId) ty ->
                handleDisconnect client ty
                CallbackResult.Continue)

            logger.LogInformation("Listening on port {Port}", port)
            try
                while not opts.CancellationToken.IsCancellationRequested do
                    if host.PollEvents(TimeSpan.FromMilliseconds(50L)) < 0 then
                        logger.LogWarning("Failed to poll network events")

                    let mutable messagesRead = 0
                    while messagesRead < 50 && messages.Reader.Count > 0 do
                        let hasMsg, msg = messages.Reader.TryRead()
                        if hasMsg then
                            match msg with
                            | WorldStarting worldId ->
                                logger.LogInformation("World {WorldId} starting", worldId)
                                // world.Messages.Writer.TryWrite(Stop) |> ignore
                            | WorldStopping _ -> failwith "Not Implemented"
                            | WorldStopped worldId ->
                                logger.LogInformation("World {WorldId} stopped", worldId)
                            | SendPacket (worldId, clientId, flags, packet) ->
                                match tryFindClient clientId with
                                | Some client when client.World <> Some worldId ->
                                    logger.LogWarning(
                                        "World {Sender} tried to send packet with ID {PacketId} to client {ClientId} but the client is in world {ClientWorld}",
                                        worldId, packet.Id, clientId, client.World)
                                | None ->
                                    logger.LogWarning(
                                        "World {Sender} tried to send packet with ID {PacketId} to client {ClientId} but the client is not assigned to any world",
                                        worldId, packet.Id, clientId)
                                | Some _ ->
                                    match sendPacket clientId flags packet with
                                    | Ok _ -> ()
                                    | Error reason ->
                                        logger.LogWarning("Failed to send packet to client {ClientId}: {Reason}",
                                            clientId, reason)
                                packet.RemoveRef()
                                ()
                        messagesRead <- messagesRead + 1

                logger.LogInformation("Stopping")
            finally
                logger.LogDebug("Stopped")
        }

    interface ISupervisor with
        member _.ServiceProvider = services
        member _.Logger = logger
        member _.LoggerFactory = loggerFactory

        member _.Clients = [||]

        // This gets JIT'd for each event type
        member _.FireEvent<'T when 'T :> IEvent>(ev : 'T) : unit =
            eventManager.Fire(ev)

        member _.SendPacket(client : ClientId, flags : PacketFlags, packet : Packet) =
            sendPacket client flags packet
            ()

        member _.GetClientStats (arg: ClientId): ClientStats option =
            raise (NotImplementedException())
