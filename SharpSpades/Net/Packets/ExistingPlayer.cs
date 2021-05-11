using SharpSpades.Entities;
using SharpSpades.Net.Packets.Attributes;
using System.Drawing;
using System.Threading.Tasks;

namespace SharpSpades.Net.Packets
{
    public partial class ExistingPlayer : Packet
    {
        public override byte Id => 9;

        public override int Length => 11 + 16;

        [Field(0)]
        public byte PlayerId { get; set; }
        [Field(1)]
        [ActualType(typeof(byte))]
        public TeamType Team { get; set; }
        [Field(2)]
        [ActualType(typeof(byte))]
        public WeaponType Weapon { get; set; }
        [Field(3)]
        public byte HeldItem { get; set; }
        [Field(4)]
        public uint Kills { get; set; }
        [Field(5)]
        public Color Color { get; set; }
        [Field(6)]
        [Length(16)]
        // TODO: Validate name
        public string Name { get; set; }

        internal override async Task HandleAsync(Client client)
        {
            client.Server.World.AddEntity(new Player(client)
            {
                Name = Name,
                IsAlive = true
            });
            await client.SendPacket(this);
        }
    }
}
