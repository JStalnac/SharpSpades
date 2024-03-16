using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using System.Numerics;

namespace SharpSpades.Net.PacketHandlers
{
    public class OrientationDataHandler : PacketHandler<OrientationData>
    {
        public override Task HandleAsync(IClient client, OrientationData packet)
        {
            if (client.IsAlive)
                client.Player.Rotation = new Vector3(packet.Orientation.X, packet.Orientation.Y, packet.Orientation.Z);
            return Task.CompletedTask;
        }
    }
}