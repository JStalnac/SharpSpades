using SharpSpades.Entities;
using SharpSpades.Net.Packets.Attributes;
using SharpSpades.Utils;
using System;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;

namespace SharpSpades.Net.Packets
{
    public sealed partial class ExistingPlayer : Packet
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
        public string Name
        {
            get => name ?? throw new InvalidOperationException("Name must not be null");
            set
            {
                Throw.IfNull(value, nameof(value));

                string clean = value.TrimEnd('\x0');
                
                if (!NameUtils.IsValidName(clean))
                    throw new ArgumentException("Invalid name");

                name = clean;
            }
        }

        private string name = null;

        internal override async Task HandleAsync(Client client)
        {
            var position = new Vector3(100f, 150f, 20f);

            client.Name = Name;
            var player = new Player(client)
            {
                Position = position
            };

            client.Player = player;
            client.Server.World!.AddEntity(player);

            foreach (var c in client.Server.Clients.Values)
                await c.SendPacketAsync(this);

            await client.SendPacketAsync(new PositionData(position));
        }
    }
}