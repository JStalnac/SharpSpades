// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades.World

open System
open System.IO
open System.IO.Compression
open System.Collections.Generic
open System.Threading
open System.Threading.Channels
open System.Diagnostics
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Serilog
open SharpSpades
open SharpSpades.Plugins
open SharpSpades.Net
open SharpSpades.World

type WorldOptions = {
    Id : WorldId
    Messages : Channel<WorldMessage>
    Output : ChannelWriter<SupervisorMessage>
    CancellationToken : CancellationToken
    Plugins : PluginDescriptor list
}

type WorldClient(id) =
    member _.Id : ClientId = id

    interface IWorldClient with
        member x.Id = x.Id

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

    let mutable map = None
    let mutable compressedMap = Unchecked.defaultof<_>
    let clients = List<WorldClient>()

    let tryFindClient id =
        let res = clients.Find (fun c -> c.Id = id)
        match res with
        | v when obj.ReferenceEquals(v, null) -> None
        | client -> Some client

    let sendSupervisor msg =
        opts.Output.TryWrite(msg) |> ignore

    let sendReliablePacket clientId packet =
        SendPacket (opts.Id, clientId, PacketFlags.Reliable, packet)
        |> sendSupervisor

    let sendUnreliablePacket clientId packet =
        SendPacket (opts.Id, clientId, PacketFlags.Unsequenced, packet)
        |> sendSupervisor

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
                // TODO: Need to inform supervisor
                return ()
            logger.LogDebug("Plugins initialised")

            logger.LogInformation("Loading map from map.vxl...")
            let! res = Map.loadMap "map.vxl"
            match res with
            | Ok m ->
                map <- Some m
                logger.LogInformation("Map loaded")
            | Error error ->
                logger.LogError("Failed to load map from file map.vxl: {Reason}",
                    sprintf "%A" error)
                // TODO: Need to inform supervisor
                return ()

            logger.LogInformation("Encoding and compressing map...")
            let sw = Stopwatch.StartNew()
            let res =
                Option.get map
                |> Map.processEncodedMap (fun memory ->
                    // TODO: This needs to happen on a separate thread because
                    // it takes 800 ms to compress the map on SmallestSize (my computer).
                    // Encoding the map also takes 100+ ms so we need a way to
                    // do it concurrently as well. We need to copy the map buffer
                    // to do so.
                    logger.LogInformation("Encoding map took {Time} ms", sw.ElapsedMilliseconds)
                    use output = new MemoryStream()
                    let compressionLevel = CompressionLevel.SmallestSize
                    use zlibStream = new ZLibStream(output, compressionLevel)
                    zlibStream.Write(memory.Span)
                    let res = output.ToArray()
                    logger.LogInformation("Original size of map: {Original} Compressed size of map: {Compressed} Compression level: {CompressionLevel}",
                        memory.Length, output.Length, compressionLevel)
                    res)
            sw.Stop()
            match res with
            | Ok c ->
                compressedMap <- c
                do! File.WriteAllBytesAsync("compressed.gz", compressedMap) |> Async.AwaitTask
                logger.LogInformation("Encoded and compressed map in {Milliseconds} ms",
                    sw.ElapsedMilliseconds)
            | Error err ->
                logger.LogError("Failed to encode and compress map: {Reason}. Took {Milliseconds} ms",
                    (sprintf "%A" err), sw.ElapsedMilliseconds)
                // TODO: Need to inform supervisor
                return ()

            while not opts.CancellationToken.IsCancellationRequested do
                let hasMsg, msg = input.TryRead()
                if hasMsg then
                    match msg with
                    | Stop ->
                        sendSupervisor (WorldStopped opts.Id)
                        return ()
                    | TransferClient clientId ->
                        let client = WorldClient(clientId)
                        clients.Add(client)
                        logger.LogInformation("Client {ClientId} connected", clientId)
                        Packets.makeMapStart (uint compressedMap.Length)
                            |> sendReliablePacket clientId
                        // TODO: OpenSpades fails to decompress the map ("unexpected EOF")
                        // TODO: BetterSpades succeeds but has an odd red color
                        // in the limbo screen and won't send ExistingPlayer
                        let chunkSize = 8 * 1024
                        let mutable rest = ReadOnlyMemory<byte>(compressedMap)
                        while rest.Length > 0 do
                            let size = min rest.Length chunkSize
                            let chunk =
                                Packets.makeMapChunk (rest.Span.Slice(0, size))
                            sendReliablePacket clientId chunk
                            rest <- rest.Slice(size)
                        logger.LogInformation("Sent all map chunks to {ClientId}", clientId)
                        Packets.makeStateDataCtf
                            1uy
                            ({ B = 0uy; G = 0uy; R = 0uy })
                            ({ B = 0uy; G = 0uy; R = 0uy })
                            ({ B = 0uy; G = 0uy; R = 0uy })
                            "Blue"
                            "Green"
                            { Team1Score = 0uy
                              Team2Score = 0uy
                              CaptureLimit = 10uy
                              Team1Intel = Packets.IntelState.OnGround { X = 256f; Y = 256f; Z = 32f }
                              Team2Intel = Packets.IntelState.OnGround { X = 256f; Y = 256f; Z = 32f }
                              Team1BasePos = { X = 0f; Y = 0f; Z = 32f }
                              Team2BasePos = { X = 512f; Y = 512f; Z = 32f }}
                        |> sendReliablePacket clientId
                        ()
                    | PacketReceived (clientId, packet) ->
                        match tryFindClient clientId with
                        | Some client ->
                            PacketHandlers.handlePacket this client packet
                        | None ->
                            logger.LogWarning("Received a packet from client {ClientId} but the client is not connected to the world",
                                clientId)
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

        member _.SendReliable (client : IWorldClient, packet : Packet): unit =
            sendReliablePacket client.Id packet
