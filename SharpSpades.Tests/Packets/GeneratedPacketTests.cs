using SharpSpades.Net.Packets;
using SharpSpades.Net.Packets.State;
using SharpSpades.Utils;
using System;
using System.Numerics;
using Xunit;
using Xunit.Abstractions;

namespace SharpSpades.Tests.Packets
{
    public class GeneratedPacketTests
    {
        private readonly ITestOutputHelper output;
        
        public GeneratedPacketTests(ITestOutputHelper output)
        {
            this.output = output;
        }
        
        [Fact]
        public void TestWrite()
        {
            var ms = new MapStart { MapSize = 1024 };

            byte[] buffer = new byte[ms.Length];
            ms.Write(buffer.AsSpan());

            byte[] expected = new byte[] { 0x0, 0x4, 0x0, 0x0 };

            Assert.Equal(expected, buffer);

            var cp = new CreatePlayer
            {
                Name = "Deuce",
                PlayerId = 0,
                Position = new Vector3(0f, 0f, 0f),
                Team = TeamType.Blue,
                Weapon = WeaponType.Rifle
            };

            buffer = new byte[cp.Length];
            cp.Write(buffer.AsSpan());

            expected = new byte[]
            {
                // ID
                0x0,

                // Weapon
                0x0,

                // Team
                (byte)TeamType.Blue,

                // Position
                0x0, 0x0, 0x0, 0x00,
                0x0, 0x0, 0x0, 0x00,
                0x0, 0x0, 0x0, 0x00,

                // Name
                0x44, 0x65, 0x75, 0x63, 0x65
            };

            output.WriteLine("Expected:");
            output.WriteLine(HexDump.Create(expected));
            output.WriteLine("Actual:");
            output.WriteLine(HexDump.Create(buffer));
            
            Assert.Equal(expected, buffer);
        }
    }
}