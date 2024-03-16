using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;

namespace SharpSpades.Net.PacketHandlers
{
    public class InputDataHandler : PacketHandler<InputData>
    {        
        public override async Task HandleAsync(IClient client, InputData packet)
        {
            if (!client.IsAlive)
                return;

            InputState inputState = packet.InputState;
            
            // Prevent client from flying by spamming the jump button
            if (client.Player.IsAirborne)
                inputState &= ~InputState.Jump;
            
            client.Player.InputState = inputState;

            await client.SendPacketAsync(new InputData
            {
                PlayerId = client.Id,
                InputState = inputState
            });
        }
    }
}