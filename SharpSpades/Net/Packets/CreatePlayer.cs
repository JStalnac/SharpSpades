using SharpSpades.Utils;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace SharpSpades.Net.Packets
{
    public class CreatePlayer : Packet
    {
        public override byte Id => 12;

        public override int Length => 1 + 1 + 1 + 12 + Name.Length;

        public byte PlayerId { get; init; }
        public WeaponType Weapon { get; init; }
        public TeamType Team { get; init; }
        public Vector3 Position { get; init; }
        // TODO: Validate name
        public string Name { get; init; }

        internal override void Read(ReadOnlySpan<byte> buffer)
            => throw new NotImplementedException();

        internal override void WriteTo(Span<byte> buffer)
        {
            buffer[0] = PlayerId;
            buffer[1] = (byte)Weapon;
            buffer.WriteSByte((sbyte)Team, 2);
            buffer.WritePosition(Position, 3);

            Span<byte> name = StringUtils.ToCP437String(Name);
            name.CopyTo(buffer.Slice(15));
        }

        internal override Task HandleAsync(Client client)
            => Task.CompletedTask;
    }
}
