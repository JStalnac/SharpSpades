using ENet.Managed;
using ENet.Managed.Async;
using Microsoft.Extensions.Logging;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Api.Utilities;
using SharpSpades.Entities;
using SharpSpades.Net.PacketHandlers;
using SharpSpades.Utils;
using SharpSpades.Vxl;
using System.Buffers;
using System.Drawing;
using System.Numerics;

#nullable enable

namespace SharpSpades.Net
{
    public class Client : IClient
    {
        public byte Id { get; }

        public string? Name
        {
            get => name;
            internal set
            {
                if (!NameUtils.IsValidName(value))
                    throw new ArgumentException($"Invalid name in \"{value}\"");

                name = value;
            }
        }

        public bool IsInLimbo => Name is null;

        public bool MapDownloadComplete { get; private set; }

        public bool IsConnected => Peer.IsConnected;

        public Server Server { get; }
        IServer IClient.Server => Server;

        public Player? Player { get; internal set; }
        IPlayer? IClient.Player => Player;

        public bool IsAlive => Player is not null;

        public TeamType? Team
        {
            get => team;
            set
            {
                if (value is null)
                    if (!IsInLimbo)
                        throw new InvalidOperationException("Cannot move player back to limbo");
                    else
                        return;

                if (!value.Value.IsValid())
                    throw new ArgumentException($"Invalid {nameof(TeamType)}");    

                team = value;
            }
        }

        public WeaponType? Weapon
        {
            get => weapon;
            set
            {
                if (value is null)
                    if (!IsInLimbo)
                        throw new InvalidOperationException("Cannot move player back to limbo");
                    else
                        return;
                
                if (!value.Value.IsValid())
                    throw new ArgumentException($"Invalid {nameof(WeaponType)}");    

                weapon = value;
            }
        }
        
        internal ENetAsyncPeer Peer { get; }

        private ILogger Logger { get; }

        private TeamType? team;
        private WeaponType? weapon;
        private string? name;
        private readonly Dictionary<byte, (Type Packet, IPacketHandler Handler)> handlers = new();

        internal Client(Server server, byte id, ENetAsyncPeer peer)
        {
            Throw.IfNull(server);
            Throw.IfNull(peer);

            Server = server;
            Id = id;
            Peer = peer;
            Logger = server.GetLogger<Client>();

            AddHandlers();
        }

        internal async Task StartAsync(CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogInformation("{Client}: Connected", this);

                Logger.LogDebug("Sending map data to {Client}", this);
                await SendMapAsync();
                Logger.LogDebug("Done sending map data to {Client}", this);

                await SendPlayersAsync();

                // Send state
                await SendPacketAsync(new StateData
                {
                    PlayerId = Id,
                    BlueName = "Blue",
                    BlueColor = Color.Blue,
                    GreenName = "Green",
                    GreenColor = Color.Green,
                    FogColor = Color.HotPink,
                    State = new CtfState()
                });

                while (IsConnected)
                {
                    // Throws if the client disconnects
                    using var rawPacket = await Peer.ReceiveAsync(stoppingToken);

                    // Process packet
                    if (rawPacket.Data.Length == 0)
                        continue;

                    byte packetId = rawPacket.Data.Span[0];

                    if (handlers.TryGetValue(packetId, out var handler))
                    {
                        // Ignore movement packets
                        if (packetId != 0
                            && packetId != 1
                            && packetId != 3)
                        {
                            Logger.LogTrace("{Client}: Received {PacketName} ({PacketId})\n{Data}",
                                this,
                                handler.Packet.Name,
                                packetId,
                                HexDump.Create(rawPacket.Data.Span).TrimEnd());
                        }

                        try
                        {
                            IPacket packet = (IPacket)Activator.CreateInstance(handler.Packet)!;
                            packet.Read(rawPacket.Data.Span.Slice(1));
                            await handler.Handler.HandleAsync(this, packet);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "{Client}: Failed to process {PacketName} ({PacketId})", this, handler.GetType().Name, packetId);
                        }
                    }
                    else
                    {
                        Logger.LogDebug("{Client}: Received packet with unknown id {PacketId}", this, packetId);
                    }
                }
            }
            catch (OperationCanceledException) { /* Server stopping */ }
            catch (ENetAsyncPeerDisconnectedException) { /* Disconnecting */ }
            catch (ENetException) { /* Packet sending failed. Already disconnected etc. */}
            catch (InvalidOperationException) { /* Peer disconnected */ }
            finally
            {
                Logger.LogDebug("{Client}: Disconnecting", this);
                
                name = null;

                if (Peer.IsConnected)
                    await Peer.DisconnectAsync((uint)DisconnectReason.Undefined);
                
                if (Player is not null)
                {
                    var player = Player;
                    Player = null;
                    Logger.LogTrace("{Client}: Destroying player", this);
                    player.Remove();
                    player.Dispose();
                }
                
                if (!stoppingToken.IsCancellationRequested)
                {
                    Logger.LogDebug("{Client}: Informing other clients of disconnection", this);
                    await this.SendToOthersAsync(new PlayerLeft { PlayerId = Id });
                }

                await Peer.Disconnection;

                Logger.LogInformation("{Client}: Disconnected", this);
            }
        }

        private async Task SendMapAsync()
        {
            // 8 kb
            const int ChunkSize = 8192;

            Map map = Server.World!.Map;

            var data = map.RawData;

            // Send Map Start
            await SendPacketAsync(new MapStart { MapSize = unchecked((uint)map.RawData.Length) });

            // Send Map Chunks
            do
            {
                int count = data.Length < ChunkSize ? data.Length : ChunkSize;

                await SendPacketAsync(new MapChunk { MapData = data.Slice(0, count) });

                data = data.Slice(count);
            } while (data.Length > 0);

            MapDownloadComplete = true;
        }

        private async Task SendPlayersAsync()
        {
            foreach (var client in Server.Clients.Values)
            {
                if (client.Name is null)
                    continue;
                
                await SendPacketAsync(new ExistingPlayer
                {
                    PlayerId = client.Id,
                    Team = client.Team!.Value,
                    Weapon = client.Weapon!.Value,
                    HeldItem = (byte)(client.Player?.Tool ?? Tool.Spade),
                    Kills = 0,
                    Color = (client.Player?.Color ?? Color.Gray),
                    Name = client.Name
                });
            }
        }

        public async Task SendPacketAsync(IPacket packet, PacketFlags packetFlags = PacketFlags.Reliable)
        {
            Throw.IfNull(packet, nameof(packet));
            ENetPacketFlags flags = packetFlags switch
            {
                PacketFlags.Unreliable => (ENetPacketFlags)0,
                PacketFlags.Reliable => ENetPacketFlags.Reliable,
                PacketFlags.Unsequenced => ENetPacketFlags.Unsequenced,
                PacketFlags.UnsequencedReliable => ENetPacketFlags.UnsequencedReliable,
                _ => throw new ArgumentException("Invalid packet flags")
            };

            // TODO: Trigger event

            int length = packet.Length + 1;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(length);

            // Limit the length of the buffer
            var memory = buffer.AsMemory().Slice(0, length);

            try
            {
                try
                {
                    memory.Span[0] = packet.Id;
                    packet.Write(memory.Span.Slice(1));
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "{Client}: Failed to write {PacketName}",
                                this, packet.GetType().Name);
                    return;
                }

                if (!Peer.IsConnected)
                    return;

                await Peer.SendAsync(0, memory, flags);

                if (packet is not MapChunk && packet is not WorldUpdate)
                {
                    Logger.LogTrace("{Client}: Sent {PacketName} ({PacketId})\n{Data}",
                        this,
                        packet.GetType().Name,
                        packet.Id,
                        HexDump.Create(memory.Span).TrimEnd());
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "{Client}: Failed to send {PacketName} ({PacketId})",
                            this, packet.GetType().Name, packet.Id);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, true);
            }
        }

        public async Task SpawnAsync(Vector3 position)
        {
            if (!IsConnected)
                return;
            if (Name is null)
                throw new InvalidOperationException("The player has not yet joined the game");
            if (IsAlive)
                throw new InvalidOperationException("The player is not dead");

            if (Team is not TeamType.Spectator)
            {
                var player = new Player(this)
                {
                    Position = position
                };

                Player = player;
                Server.World!.AddEntity(player);
            }

            await Server.BroadcastPacketAsync(new CreatePlayer
            {
                Position = position,
                Name = Name,
                PlayerId = Id,
                Team = Team!.Value,
                Weapon = Weapon!.Value
            });
        }

        /// <summary>
        /// Disconnects the client.
        /// </summary>
        /// <param name="reason"></param>
        public async ValueTask DisconnectAsync(DisconnectReason reason)
        {
            Logger.LogDebug("{Client}: Disconnection requested", this);
            await Peer.DisconnectAsync((uint)reason);
        }

        public override string ToString()
        {
            // <#0>
            // <#0 'Player'>
            return $"<#{Id}{(Name is not null ?$" '{Name}'" : "")}>";
        }

        private void AddHandlers()
        {
            AddHandler(new PositionDataHandler());
            AddHandler(new ExistingPlayerHandler());
            AddHandler(new InputDataHandler());
            AddHandler(new OrientationDataHandler());
            AddHandler(new ChangeTeamHandler());
            AddHandler(new ChangeWeaponHandler());
            AddHandler(new SetToolHandler());
            AddHandler(new SetColorHandler());
            AddHandler(new WeaponInputHandler());
            AddHandler(new HitPacketHandler());
            AddHandler(new ChatMessageHandler());

            void AddHandler<T>(PacketHandler<T> handler) where T : IPacket
            {
                IPacket packet = Activator.CreateInstance<T>();
                handlers.Add(packet.Id, (typeof(T), handler));
            }
        }
    }
}