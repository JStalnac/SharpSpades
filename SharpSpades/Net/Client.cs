﻿using ENet.Managed;
using ENet.Managed.Async;
using Microsoft.Extensions.Logging;
using SharpSpades.Entities;
using SharpSpades.Net.Packets;
using SharpSpades.Net.Packets.State;
using SharpSpades.Utils;
using SharpSpades.Vxl;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace SharpSpades.Net
{
    public class Client
    {
        public byte Id { get; }

        public string? Name
        {
            get => name;
            internal set
            {
                Throw.IfNull(value, nameof(value));
                Throw.IfNotNull(name, $"The {nameof(name)} is already set!");

                if (!NameUtils.IsValidName(value))
                    throw new ArgumentException($"Invalid name in \"{value}\"");

                name = value;
            }
        }

        public bool IsInLimbo => Name is null;

        public Server Server { get; }

        public Player? Player { get; internal set; }

        internal event Action<ENetAsyncPeer>? Disconnected;

        private ILogger Logger { get; }

        private string? name;
        private readonly TaskCompletionSource<bool> DisconnectCompletionSource = new();
        private readonly CancellationTokenSource cts = new();
        private readonly ENetAsyncPeer peer;
        private readonly Dictionary<byte, Packet> packets = new();

        public Client(Server server, ENetAsyncPeer peer, byte id)
        {
            Throw.IfNull(peer, nameof(peer));
            Throw.IfNull(server, nameof(server));

            Server = server;
            this.peer = peer;
            Logger = server.GetLogger<Client>();
            Id = id;

            AddPackets();
        }

        internal async Task StartAsync()
        {
            // We will get stuck in peer.ReceiveAsync if the peer disconnects.
            _ = peer.Disconnection.ContinueWith(_ =>
            {
                Logger.LogInformation("#{0}: Disconnecting...", Id);
                cts.Cancel();
            });

            try
            {
                await peer.Connection;

                Logger.LogInformation("#{0}: Connected", Id);

                var cancellationToken = cts.Token;

                Logger.LogDebug("Sending map data to #{0}", Id);
                await SendMapAsync();
                Logger.LogDebug("Done sending map data to #{0}", Id);

                await SendPlayersAsync();

                // Send state
                await SendPacketAsync(new StateData
                {
                    PlayerId = Id,
                    BlueColor = Color.Blue,
                    GreenColor = Color.Green,
                    FogColor = Color.HotPink,
                    State = new CtfState()
                });

                while (!cts.IsCancellationRequested && peer.IsConnected)
                {
                    using var rawPacket = await peer.ReceiveAsync(cancellationToken);

                    // Process packet
                    byte packetId = rawPacket.Data.Span[0];

                    // Movement packets
                    if (packetId is 0 or 1 or 3)
                        continue;

                    if (packets.TryGetValue(packetId, out var packet))
                    {
                        Logger.LogTrace("#{0}: Received {1} ({2})\n{3}",
                            Id,
                            packet.GetType().Name,
                            packetId,
                            HexDump.Create(rawPacket.Data.Span).TrimEnd());

                        try
                        {
                            packet.Read(rawPacket.Data.Span.Slice(1));
                            await packet.HandleAsync(this);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "#{0}: Failed to process {1} ({2})", Id, packet.GetType().Name, packetId);
                        }
                    }
                    else
                        Logger.LogDebug("#{0}: Received packet with unknown id {1}", Id, packetId);
                }
            }
            catch (ENetAsyncPeerDisconnectedException)
            {
                // Disconnecting
            }
            catch (OperationCanceledException) { }
            finally
            {
                Logger.LogInformation("#{0}: Disconnected", Id);
                Disconnected?.Invoke(peer);
            }
        }

        private async Task SendMapAsync()
        {
            // 8 kb
            const int ChunkSize = 8192;

            Map map = Server.World!.Map;

            int dataLength = map.RawData.Length;
            var data = map.RawData.AsMemory();

            // Send Map Start
            await SendPacketAsync(new MapStart { MapSize = unchecked((uint)dataLength) });

            // Send Map Chunks
            do
            {
                int count = data.Length < ChunkSize ? data.Length : ChunkSize;

                await SendPacketAsync(new MapChunk { MapData = data.Slice(0, count) });

                data = data.Slice(count);
            } while (data.Length > 0);
        }

        private async Task SendPlayersAsync()
        {
            var random = new Random();
            foreach (var client in Server.Clients.Values)
            {
                if (client.Name is null)
                    continue;
                
                await SendPacketAsync(new ExistingPlayer
                {
                    PlayerId = client.Id,
                    Team = TeamType.Blue,
                    Weapon = WeaponType.Rifle,
                    HeldItem = 2,
                    Kills = 0,
                    Color = Color.FromArgb(random.Next()),
                    Name = client.Name
                });
            }
        }

        public async ValueTask SendPacketAsync(Packet packet)
        {
            Throw.IfNull(packet, nameof(packet));

            // TODO: Trigger event

            int length = packet.Length + 1;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(length);

            // Limit the length of the buffer
            var memory = ((Memory<byte>)buffer).Slice(0, length);

            try
            {
                try
                {
                    memory.Span[0] = packet.Id;
                    packet.Write(memory.Span.Slice(1));
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "#{0}: Failed to write {1}", Id, packet.GetType().Name);
                    return;
                }

                // TODO: More packet flags
                await peer.SendAsync(0, memory, ENetPacketFlags.Reliable);

                if (packet is not MapChunk)
                {
                    Logger.LogTrace("#{0}: Sent {1} ({2})\n{3}",
                        Id,
                        packet.GetType().Name,
                        packet.Id,
                        HexDump.Create(memory.Span).TrimEnd());
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "#{0} Failed to send {1}", Id, packet.GetType().Name);
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, true);
            }
        }

        /// <summary>
        /// Disconnects the client.
        /// </summary>
        /// <param name="reason"></param>
        public async Task DisconnectAsync(DisconnectReason reason) => await peer.DisconnectAsync((uint)reason);

        private void AddPackets()
        {
            AddPacket(new ExistingPlayer());
            AddPacket(new InputData());
            AddPacket(new OrientationData());

            void AddPacket(Packet packet)
            {
                packets.Add(packet.Id, packet);
            }
        }
    }
}