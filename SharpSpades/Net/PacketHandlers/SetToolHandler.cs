using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Api.Utilities;
using SharpSpades.Utils;

namespace SharpSpades.Net.PacketHandlers
{
    public class SetToolHandler : PacketHandler<SetTool>
    {
        public override async Task HandleAsync(IClient client, SetTool packet)
        {
            if (!packet.Tool.IsValid())
                return;
            if (!client.IsAlive)
                return;

            client.Player.Tool = packet.Tool;
            
            await client.SendToOthersAsync(new SetTool
            {
                PlayerId = client.Id,
                Tool = packet.Tool
            });
        }
    }
}