using SharpSpades.Api.Net.Packets;
using SharpSpades.Entities;
using System.Numerics;

namespace SharpSpades.Api.Net
{
    public interface IClient
    {
        byte Id { get; }

        string? Name { get; }

        bool IsInLimbo { get; }

        bool MapDownloadComplete { get; }

        bool IsConnected { get; }

        IServer Server { get; }

        IPlayer? Player { get; }

        bool IsAlive { get; }

        TeamType? Team { get; set; }

        WeaponType? Weapon { get; set; }

        Task SendPacketAsync(IPacket packet, PacketFlags packetFlags = PacketFlags.Reliable);

        Task SpawnAsync(Vector3 position);

        /// <summary>
        /// Disconnects the client.
        /// </summary>
        /// <param name="reason"></param>
        ValueTask DisconnectAsync(DisconnectReason reason);
    }
}