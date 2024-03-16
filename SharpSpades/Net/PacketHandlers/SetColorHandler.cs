using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Api.Utilities;

namespace SharpSpades.Net.PacketHandlers
{
    public class SetColorHandler : PacketHandler<SetColor>
    {
        public override async Task HandleAsync(IClient client, SetColor packet)
        {
            if (!client.IsAlive)
                return;
            
            client.Player.Color = packet.Color;
            
            await client.SendToOthersAsync(new SetColor
            {
                PlayerId = client.Id,
                Color = packet.Color
            });
        }
    }
}