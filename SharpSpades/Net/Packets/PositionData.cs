using SharpSpades.Net.Packets.Attributes;
using System.Numerics;
using System.Threading.Tasks;

namespace SharpSpades.Net.Packets
{
    public partial class PositionData : Packet
    {
        public override byte Id => 0;
        public override int Length => 12;

        // TODO: Check NaN
        [Field(0)]
        public float X { get; set; }
        [Field(1)]
        public float Y { get; set; }
        [Field(2)]
        public float Z { get; set; }

        public PositionData() { }

        public PositionData(Vector3 position)
        {
            X = position.X;
            Y = position.Y;
            Z = position.Z;
        }

        internal override Task HandleAsync(Client client)
            => Task.CompletedTask;
    }
}