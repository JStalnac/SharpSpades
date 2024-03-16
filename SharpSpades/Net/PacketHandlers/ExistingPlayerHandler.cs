using Microsoft.Extensions.Logging;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Utils;
using System.Numerics;

namespace SharpSpades.Net.PacketHandlers
{
    public class ExistingPlayerHandler : PacketHandler<ExistingPlayer>
    {
        public override async Task HandleAsync(IClient client, ExistingPlayer packet)
        {
            if (!client.IsInLimbo && client.Team is not TeamType.Spectator)
            {
                client.Server.GetLogger<ExistingPlayer>()
                    .LogWarning("{Client} tried to send ExistingPlayer while already in game", client);
                return;
            }

            string clean = packet.Name.TrimEnd('\x0');
            
            if (!NameUtils.IsValidName(clean))
            {
                await client.DisconnectAsync(DisconnectReason.Kicked);
                return;
            }

            if (client is Client c)
            {
                c.Name = clean;

                // TODO: Check values
                c.Team = packet.Team;
                c.Weapon = packet.Weapon;
            }

            await client.Server.BroadcastPacketAsync(packet);
            await client.SpawnAsync(new Vector3(100f, 150f, 20f));
        }
    }
}