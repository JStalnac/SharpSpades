using SharpSpades.Api.Utils;
using System;
using System.Drawing;

namespace SharpSpades.Api.Net.Packets
{
    public class ExistingPlayer : IPacket
    {
        public byte Id => 9;

        public int Length => 11 + Name.Length;

        public byte Team { get; private set; }
        public byte Weapon { get; private set; }
        public byte HeldItem { get; private set; }
        public uint Kills { get; private set; }
        public Color Color { get; private set; }
        public string Name { get; private set; }

        public void Read(ReadOnlySpan<byte> buffer)
        {
            Team = buffer[0];
            Weapon = buffer[1];
            HeldItem = buffer[2];

            Kills = buffer.ReadUInt32LittleEndian(4);
            Color = buffer.ReadColor(8);

            // TODO: Check if name is invalid
            Name = StringUtils.ReadCP437String(buffer.Slice(11));
        }

        public void WriteTo(Span<byte> buffer)
        {
            throw new NotImplementedException();
        }
    }
}
