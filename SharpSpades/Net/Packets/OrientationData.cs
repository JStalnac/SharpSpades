using SharpSpades.Net.Packets.Attributes;
using System.Numerics;
using System.Threading.Tasks;

namespace SharpSpades.Net.Packets
{
    [ReadOnly]
    public sealed partial class OrientationData : Packet
    {
        public override byte Id => 1;
        public override int Length => 12;
        
        [Field(0)]
        public float X { get; set; }
        [Field(1)]
        public float Y { get; set; }
        [Field(2)]
        public float Z { get; set; }

        internal override Task HandleAsync(Client client)
        {
            if (client.Player is not null)
                client.Player.Rotation = new Quaternion(X, Y, Z, 1);
            return Task.CompletedTask;
        }
    }
}