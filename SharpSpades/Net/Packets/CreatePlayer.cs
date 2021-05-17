using SharpSpades.Net.Packets.Attributes;
using SharpSpades.Utils;
using System;
using System.Numerics;
using System.Threading.Tasks;

#nullable enable

namespace SharpSpades.Net.Packets
{
    [WriteOnly]
    public sealed partial class CreatePlayer : Packet
    {
        public override byte Id => 12;

        public override int Length => 1 + 1 + 1 + 12 + this.Name.Length;

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
        [Field(4)]
        public string Name
        {
            get => name ?? throw new InvalidOperationException("Name must not be null");
            set
            {
                Throw.IfNull(value);
                if (!NameUtils.IsValidName(value))
                    throw new ArgumentException("Invalid name");
                name = value;
            }
        }

        private string? name;

        internal override Task HandleAsync(Client client)
            => Task.CompletedTask;
    }
}
