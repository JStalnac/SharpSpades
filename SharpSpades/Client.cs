﻿using ENet.Managed;
using ENet.Managed.Async;
using Microsoft.Extensions.Logging;
using SharpSpades.Api;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Api.Net.Packets.State;
using SharpSpades.Api.Utils;
using SharpSpades.Vxl;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace SharpSpades.Net
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
                    Logger.LogDebug("#{0}: Received packet with id {1}", Id, packetId);
                }
            }
            catch (OperationCanceledException)
            {
                // Disconnecting
            }

            if (cts.IsCancellationRequested)
                Logger.LogDebug("#{0}: Cancellation token requested cancellation", Id);

            Logger.LogInformation("#{0}: Disconnected", Id);
            Disconnected?.Invoke(peer);
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
                
                data = data.Slice(count);
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
            
            await Task.CompletedTask;
        }

        public async ValueTask SendPacket(IPacket packet)
        {
            Throw.IfNull(packet, nameof(packet));

            // TODO: Trigger event

            using var ms = new MemoryStream();

            try
            {
                ms.WriteByte(packet.Id);
                packet.WriteTo(ms);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, $"Failed to write packet {packet.GetType()}");
                return;
            }
            
            // TODO: More packet flags
            await peer.SendAsync(0, ms.GetBuffer(), ENetPacketFlags.Reliable);
        }

        /// <summary>
        /// Disconnects the client.
        /// </summary>
        /// <param name="reason"></param>
        public ValueTask DisconnectAsync(DisconnectReason reason)
            => peer.DisconnectAsync((uint)reason);
    }
}