using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Api.Utilities;

namespace SharpSpades.Net.PacketHandlers
{
    public class WeaponInputHandler : PacketHandler<WeaponInput>
    {
        public override async Task HandleAsync(IClient client, WeaponInput packet)
        {
            if (!client.IsAlive)
                return;
            client.Player.PrimaryFire = packet.PrimaryFire;
            client.Player.SecondaryFire = packet.SecondaryFire;
            
            await client.SendToOthersAsync(new WeaponInput
            {
                PlayerId = client.Id,
                PrimaryFire = packet.PrimaryFire,
                SecondaryFire = packet.SecondaryFire
            });
        }
    }
}