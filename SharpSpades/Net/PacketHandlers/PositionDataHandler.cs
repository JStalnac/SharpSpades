using Microsoft.Extensions.Logging;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using System.Numerics;

namespace SharpSpades.Net.PacketHandlers
{
    public class PositionDataHandler : PacketHandler<PositionData>
    {
        public override async Task HandleAsync(IClient client, PositionData packet)
        {
            if (!client.IsAlive)
                return;
            var pos = new Vector3(packet.Position.X, packet.Position.Y, packet.Position.Z);
            var old = client.Player.Position;
            if (Single.IsNaN(pos.X) || Single.IsNaN(pos.Y) || Single.IsNaN(pos.Z))
            {
                client.Server.GetLogger<PositionData>().LogWarning("{Client} Sent an invalid position packet. Position: {Pos} Real position: {Old}",
                        client, pos, old);
                await client.SendPacketAsync(new PositionData(old));
                return;
            }
            if (Vector3.Distance(old, pos) > 3)
            {
                client.Server.GetLogger<PositionData>().LogWarning("{Client} tried to set its position too far. Position: {Pos} Real position: {Old}",
                                    client, pos, old);
                await client.SendPacketAsync(new PositionData(old));
                return;
            }
            client.Player.Position = pos;
        }
    }
}