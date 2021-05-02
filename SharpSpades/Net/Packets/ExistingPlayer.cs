using SharpSpades.Entities;
using SharpSpades.Utils;
using System;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;

namespace SharpSpades.Net.Packets
{
    public class ExistingPlayer : IPacket
    {
        public byte Id => 9;

        public int Length => 11 + Name.Length;

        public byte PlayerId { get; set; }
        public TeamType Team { get; set; }
        public WeaponType Weapon { get; set; }
        public byte HeldItem { get; set; }
        public uint Kills { get; set; }
        public Color Color { get; set; }
        public string Name { get; set; }

        public void Read(ReadOnlySpan<byte> buffer)
        {
            PlayerId = buffer[0];
            Team = (TeamType)buffer[1];
            Weapon = (WeaponType)buffer[2];
            HeldItem = buffer[3];

            Kills = buffer.ReadUInt32LittleEndian(4);
            Color = buffer.ReadColor(9);

            // TODO: Check if name is invalid
            Name = StringUtils.ReadCP437String(buffer.Slice(12));
        }

        public void WriteTo(Span<byte> buffer)
        {
            buffer[0] = PlayerId;
            buffer[1] = (byte)Team;
            buffer[2] = (byte)Weapon;
            buffer[3] = HeldItem;
            buffer.WriteUInt32LittleEndian(Kills, 4);
            buffer.WriteColor(Color, 8);
            StringUtils.ToCP437String(Name).AsSpan().CopyTo(buffer.Slice(11));
        }

        public async Task HandleAsync(Client client)
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
