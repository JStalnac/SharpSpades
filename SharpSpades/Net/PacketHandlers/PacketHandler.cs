using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;

namespace SharpSpades.Net.PacketHandlers
{
    public abstract class PacketHandler<T> : IPacketHandler where T : IPacket
    {
        Task IPacketHandler.HandleAsync(IClient client, IPacket packet)
            => HandleAsync(client, (T)packet);

        public abstract Task HandleAsync(IClient client, T packet);
    }
}