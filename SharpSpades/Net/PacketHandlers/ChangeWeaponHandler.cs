using Microsoft.Extensions.Logging;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Utils;

namespace SharpSpades.Net.PacketHandlers
{
    public class ChangeWeaponHandler : PacketHandler<ChangeWeapon>
    {
        public override async Task HandleAsync(IClient client, ChangeWeapon packet)
        {            
            if (client.IsInLimbo)
                return;
            if (!packet.Weapon.IsValid())
                return;
            
            var logger = client.Server.GetLogger<ChangeWeapon>();
            logger.LogInformation("{Client} is changing weapons", client);
            
            client.Weapon = packet.Weapon;

            logger.LogInformation("Killing {Client}", client);
            await client.Player.KillAsync(client.Id, KillType.WeaponChange, 5);

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