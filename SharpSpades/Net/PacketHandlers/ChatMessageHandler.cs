using Microsoft.Extensions.Logging;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;

namespace SharpSpades.Net.PacketHandlers;

public class ChatMessageHandler : PacketHandler<ChatMessage>
{
    public override async Task HandleAsync(IClient client, ChatMessage packet)
    {
        // Client shouldn't send system messages
        if (packet.Type == ChatType.System)
            return;

        var logger = client.Server.GetLogger<ChatMessageHandler>();
        logger.LogInformation("{Client} <{Type}>: {Message}", client, packet.Type, packet.Message);

        if (packet.Message.ToLower().Contains("hi"))
        {
            logger.LogInformation("Client {Client} said hi", client);
            await client.SendPacketAsync(new ChatMessage("Hello!"));
        }
    }
}