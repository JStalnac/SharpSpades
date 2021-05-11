using SharpSpades.Net.Packets.Attributes;
using System.Numerics;
using System.Threading.Tasks;

namespace SharpSpades.Net.Packets
{
    [WriteOnly]
    public partial class CreatePlayer : Packet
    {
        public override byte Id => 12;

        public override int Length => 1 + 1 + 1 + 12 + Name.Length;

        [Field(0)]
        public byte PlayerId { get; set; }
        [Field(1)]
        [ActualType(typeof(byte))]
        public WeaponType Weapon { get; set; }
        [Field(2)]
        [ActualType(typeof(byte))]
        public TeamType Team { get; set; }
        [Field(3)]
        public Vector3 Position { get; set; }
        // TODO: Validate name
        [Field(4)]
        public string Name { get; set; }

        internal override Task HandleAsync(Client client)
            => Task.CompletedTask;
    }
}
