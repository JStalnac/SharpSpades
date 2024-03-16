using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;

namespace SharpSpades.Net.PacketHandlers
{
    public interface IPacketHandler
    {
        Task HandleAsync(IClient client, IPacket packet);
    }
}