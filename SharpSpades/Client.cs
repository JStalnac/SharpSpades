using ENet.Managed;
using ENet.Managed.Async;
using Microsoft.Extensions.Logging;
using SharpSpades.Api;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Api.Net.Packets.State;
using SharpSpades.Api.Utils;
using SharpSpades.Vxl;
using System;
using System.Buffers;
using System.Drawing;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace SharpSpades
{
    public class Client : IClient
    {
        public byte Id { get; }
        public IServer Server { get; }
        public IPlayer? Player { get; internal set; }
        internal event Action<ENetAsyncPeer>? Disconnected;
        private ILogger Logger { get; }

        private readonly TaskCompletionSource<bool> DisconnectCompletionSource = new();
        private readonly CancellationTokenSource cts = new();
        private readonly ENetAsyncPeer peer;
        
        public Client(Server server, ENetAsyncPeer peer, byte id)
        {
            Throw.IfNull(peer, nameof(peer));
            Throw.IfNull(server, nameof(server));
            Server = server;
            this.peer = peer;
            Logger = server.GetLogger<Client>();
            Id = id;
        }
        
        internal async Task StartAsync()
        {
            // We will get stuck in peer.ReceiveAsync if the peer disconnects.
            _ = peer.Disconnection.ContinueWith(t =>
            {
                Logger.LogInformation("#{0}: Disconnecting...", Id);
                cts.Cancel();
            });

            await peer.Connection;

            // NOTE: Maybe not needed
            if (!peer.IsConnected)
                return;

            Logger.LogInformation("#{0}: Connected", Id);

            var cancellationToken = cts.Token;

            Logger.LogDebug("Sending map data to #{0}", Id);
            await SendMap();
            
            try
            {
                while (!cts.IsCancellationRequested && peer.IsConnected)
                {
                    using var packet = await peer.ReceiveAsync(cancellationToken);
                    
                    // Process packet
                    byte packetId = packet.Data.Span[0];
                    Logger.LogDebug("#{0}: Received packet with id {1}\n{2}", Id, packetId, HexDump.Create(packet.Data.Span).TrimEnd());

                    if (packetId == 9)
                    {
                        // Existing player
                        var existing = new ExistingPlayer();
                        existing.Read(packet.Data.Span);

                        await SendPacket(new CreatePlayer
                        {
                            PlayerId = Id,
                            Weapon = WeaponType.Rifle,
                            Team = TeamType.Blue,
                            Position = new Vector3(200f, 200f, 0f),
                            Name = existing.Name
                        });
                    }
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

        private async Task SendMap()
        {
            // 8 kb
            const int ChunkSize = 8192;

            Map map = ((Server)Server).Map!;

            int dataLength = map.RawData.Length;
            ReadOnlyMemory<byte> data = map.RawData.AsMemory();

            // Send Map Start
            await SendPacket(new MapStart
            {
                MapSize = unchecked((uint)dataLength)
            });

            // Send Map Chunks
            do
            {
                int count = data.Length < ChunkSize ? data.Length : ChunkSize;

                await SendPacket(new MapChunk
                {
                    MapData = data.Slice(0, count)
                });
                
                data = data.Slice(start: count);
            } while (data.Length > 0);

            // Send state
            await SendPacket(new StateData
            {
                PlayerId = Id,
                BlueColor = Color.Blue,
                GreenColor = Color.Green,
                FogColor = Color.HotPink,
                State = new CtfState()
            });
        }

        public async ValueTask SendPacket(IPacket packet)
        {
            Throw.IfNull(packet, nameof(packet));

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
                    packet.WriteTo(memory.Span.Slice(1));
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "#{0}: Failed to write packet {1}", Id, packet.GetType());
                    return;
                }

                // TODO: More packet flags
                await peer.SendAsync(0, memory, ENetPacketFlags.Reliable);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "#{0} Failed to send packet {1}", Id, packet.GetType());
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
        public ValueTask DisconnectAsync(DisconnectReason reason)
            => peer.DisconnectAsync((uint)reason);
    }
}
