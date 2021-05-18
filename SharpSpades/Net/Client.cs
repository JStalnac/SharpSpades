using ENet.Managed;
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
            get => this.name;
            internal set
            {
                Throw.IfNull(value, nameof(value), StringUtils.GenerateNullExceptionMessage());
                Throw.IfNotNull(name, $"The {nameof(name)} is already set!");

                if (!NameUtils.IsValidName(value))
                    throw new ArgumentException($"Invalid name in \"{value}\"");

                this.name = value;
            }
        }

        public bool IsInLimbo => this.Name is null;

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
            Throw.IfNull(peer, nameof(peer), StringUtils.GenerateNullExceptionMessage());
            Throw.IfNull(server, nameof(server), StringUtils.GenerateNullExceptionMessage());

            this.Server = server;
            this.peer = peer;
            this.Logger = server.GetLogger<Client>();
            this.Id = id;

            this.AddPackets();
        }

        internal async Task StartAsync()
        {
            // We will get stuck in peer.ReceiveAsync if the peer disconnects.
            _ = peer.Disconnection.ContinueWith(_ =>
            {
                this.Logger.LogInformation("#{0}: Disconnecting...", this.Id);
                cts.Cancel();
            });

            try
            {
                await peer.Connection;

                this.Logger.LogInformation("#{0}: Connected", this.Id);

                var cancellationToken = cts.Token;

                this.Logger.LogDebug("Sending map data to #{0}", this.Id);
                await this.SendMapAsync();
                this.Logger.LogDebug("Done sending map data to #{0}", this.Id);

                await this.SendPlayersAsync();

                // Send state
                await this.SendPacketAsync(new StateData
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
                        this.Logger.LogTrace("#{0}: Received {1} ({2})\n{3}",
                                             this.Id,
                                             packet.GetType().Name,
                                             packetId,
                                             HexDump.Create(rawPacket.Data.Span).TrimEnd());

                        try
                        {
                            packet.Read(rawPacket.Data.Span[1..]);
                            await packet.HandleAsync(this);
                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogWarning(ex, "#{0}: Failed to process {1} ({2})", this.Id, packet.GetType().Name, packetId);
                        }
                    }
                    else
                        this.Logger.LogDebug("#{0}: Received packet with unknown id {1}", this.Id, packetId);
                }
            }
            catch (ENetAsyncPeerDisconnectedException)
            {
                // Disconnecting
            }
            catch (OperationCanceledException) { }
            finally
            {
                this.Logger.LogInformation("#{0}: Disconnected", this.Id);
                Disconnected?.Invoke(peer);
            }
        }

        private async Task SendMapAsync()
        {
            // 8 kb
            const int ChunkSize = 8192;

            Map map = this.Server.World!.Map;

            int dataLength = map.RawData.Length;
            ReadOnlyMemory<byte> data = map.RawData.AsMemory();

            // Send Map Start
            await this.SendPacketAsync(new MapStart
            {
                MapSize = unchecked((uint)dataLength)
            });

            // Send Map Chunks
            do
            {
                int count = data.Length < ChunkSize ? data.Length : ChunkSize;

                await this.SendPacketAsync(new MapChunk
                {
                    MapData = data.Slice(0, count)
                });

                data = data[count..];
            } while (data.Length > 0);
        }

        private async Task SendPlayersAsync()
        {
            var random = new Random();
            foreach (var client in this.Server.Clients.Values)
            {
                // WIP

                // if (client.Player is not null)
                // {
                    await this.SendPacketAsync(new ExistingPlayer
                    {
                        PlayerId = 1,
                        Team = TeamType.Blue,
                        Weapon = WeaponType.Rifle,
                        HeldItem = 2,
                        Kills = 0,
                        Color = Color.FromArgb(random.Next()),
                        Name = $"Deuce {client.Id}"
                    });
                // }
            }
        }

        public async ValueTask SendPacketAsync(Packet packet)
        {
            Throw.IfNull(packet, nameof(packet), StringUtils.GenerateNullExceptionMessage());

            // TODO: Trigger event

            int length = packet.Length + 1;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(length);

            // Limit the length of the buffer
            Memory<byte> memory = ((Memory<byte>)buffer).Slice(0, length);

            try
            {
                try
                {
                    memory.Span[0] = packet.Id;
                    packet.Write(memory.Span[1..]);
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "#{0}: Failed to write {1}", this.Id, packet.GetType().Name);
                    return;
                }

                // TODO: More packet flags
                await peer.SendAsync(0, memory, ENetPacketFlags.Reliable);

                if (packet is not MapChunk)
                    this.Logger.LogTrace("#{0}: Sent {1} ({2})\n{3}",
                                         this.Id,
                                         packet.GetType().Name,
                                         packet.Id,
                                         HexDump.Create(memory.Span).TrimEnd());
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "#{0} Failed to send {1}", this.Id, packet.GetType().Name);
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
        public async Task DisconnectAsync(DisconnectReason reason) => await this.peer.DisconnectAsync((uint)reason);

        private void AddPackets()
        {
            AddPacket(new ExistingPlayer());

            void AddPacket(Packet packet) => this.packets.Add(packet.Id, packet);
        }
    }
}
