using Microsoft.Extensions.Logging;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Utils;

namespace SharpSpades.Net.PacketHandlers
{
    public class ChangeTeamHandler : PacketHandler<ChangeTeam>
    {
        public override async Task HandleAsync(IClient client, ChangeTeam packet)
        {
            if (client.IsInLimbo)
                return;
            if (!packet.Team.IsValid())
                return;

            var logger = client.Server.GetLogger<ChangeTeam>();
            logger.LogInformation("{Client} is changing teams", client);
            
            client.Team = packet.Team;

            logger.LogInformation("Killing {Client}", client);
            await client.Player.KillAsync(client.Id, KillType.TeamChange, 5);

            _ = Spawn();

            async Task Spawn()
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                logger.LogInformation("{Client}: Spawning", client);
                await client.SpawnAsync(new System.Numerics.Vector3(100f, 150f, 20f));
            }
        }
    }
}