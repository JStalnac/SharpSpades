using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpSpades.Api;
using SharpSpades.Api.Entities;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using System.Collections.Immutable;

namespace SharpSpades
{
    public interface IServer
    {
        IConfiguration Configuration { get; }

        ImmutableDictionary<byte, IClient> Clients { get; }

        IWorld? World { get; }

        const short MaxPlayers = 32;

        ILogger<T> GetLogger<T>();

        ILogger GetLogger(string categoryName);

        /// <summary>
        /// Sends the packet to all connected clients that have completed the map download.
        /// </summary>
        /// <param name="packet">The packet to send</param>
        /// <param name="packetFlags">Packet flags for sending the packet. Defaults to <see cref="PacketFlags.Reliable"/>.</param>
        /// <param name="quiet">Passed to <see cref="Client.SendPacketAsync(Packet, PacketFlags, bool)"/>.</param>
        /// <remarks>Does not throw exceptions that may occur when sending the packet to clients.</remarks>
        /// <returns></returns>
        Task BroadcastPacketAsync(IPacket packet, PacketFlags packetFlags = PacketFlags.Reliable);
    }
}