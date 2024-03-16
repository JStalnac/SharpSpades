using Microsoft.Extensions.Logging;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Native;
using System.Numerics;

namespace SharpSpades.Net.PacketHandlers
{
    public class HitPacketHandler : PacketHandler<HitPacket>
    {
        public override async Task HandleAsync(IClient client, HitPacket packet)
        {
            if (!client.IsAlive)
                return;

            var target = client.Server.Clients.Values
                    .First(c => c.Id == packet.Target);
            if (!target.IsAlive)
                return;
            
            var logger = client.Server.GetLogger<HitPacket>();

            if (!client.Player.PrimaryFire)
            {
                logger.LogWarning("{Client} tried to hit without shooting", client);
                return;
            }

            // TODO: Check if the client is shooting early

            var position = client.Player.Position;
            var eyePosition = client.Player.EyePosition;
            var orientation = client.Player.Rotation;
            var targetPosition = target.Player.Position;
            float length = Vector3.Distance(position, targetPosition);

            var map = client.Server.World.Map;
            if (!LibSharpSpades.ValidateHit(position, orientation, targetPosition, 5f)
                && (!map.CastRay(eyePosition, orientation, length, out var _)
                || !map.CastRay(eyePosition, orientation, length, out var _)))
            {
                target.Player.ApplyDamage(20);
                await target.SendPacketAsync(new SetHp
                {
                    Health = target.Player.Health,
                    Source = client.Player.Position,
                    Type = DamageType.Weapon
                });
            }
            else
            {
                client.Server.GetLogger<HitPacket>()
                    .LogWarning("{Client} failed hit test at Position: {Pos} Orientation: {Orientation}; Target: {Target}",
                            client, position, orientation, targetPosition);
            }
        }
    }
}