using SharpSpades.Net.Packets.Attributes;
using System.Threading.Tasks;

namespace SharpSpades.Net.Packets
{
    public sealed partial class InputData : Packet
    {
        public override byte Id => 3;
        public override int Length => 2;

        [Field(0)]
        public byte PlayerId { get; private set; }

        [Field(1)]
        [ActualType(typeof(byte))]
        public InputState InputState { get; private set; }

        internal override Task HandleAsync(Client client)
        {
            if (!client.IsInLimbo)
                client.Player.InputState = InputState;
            return Task.CompletedTask;

            // TODO
            // foreach (var c in client.Server.Clients.Values)
            //     await c.SendPacketAsync(this);
        }
    }
}