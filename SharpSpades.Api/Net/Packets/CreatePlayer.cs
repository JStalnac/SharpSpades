using SharpSpades.Api.Utils;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace SharpSpades.Api.Net.Packets
{
    public class CreatePlayer : IPacket
    {
        public byte Id => 12;

        public int Length => 1 + 1 + 1 + 12 + Name.Length;

        public byte PlayerId { get; init; }
        public WeaponType Weapon { get; init; }
        public TeamType Team { get; init; }
        public Vector3 Position { get; init; }
        // TODO: Validate name
        public string Name { get; init; }

        public void Read(ReadOnlySpan<byte> buffer)
            => throw new NotImplementedException();

        public void WriteTo(Span<byte> buffer)
        {
            buffer[0] = PlayerId;
            buffer[1] = (byte)Weapon;
            buffer.WriteSByte((sbyte)Team, 2);
            buffer.WritePosition(Position, 3);

            Span<byte> name = StringUtils.ToCP437String(Name);
            name.CopyTo(buffer.Slice(15));
        }

        public Task HandleAsync(IClient client)
            => Task.CompletedTask;
    }
}
