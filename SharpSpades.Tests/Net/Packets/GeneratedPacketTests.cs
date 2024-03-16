using SharpSpades.Api;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Utils;
using System.Drawing;
using Xunit;
using Xunit.Abstractions;

namespace SharpSpades.Tests.Net.Packets
{
    public class GeneratedPacketTests
    {
        private readonly ITestOutputHelper output;
        
        public GeneratedPacketTests(ITestOutputHelper output)
        {
            this.output = output;
        }
        
        [Fact]
        public void Test_Write_KillAction()
        {
            var killAction = new KillAction
            {
                PlayerId = 1,
                KillerId = 2,
                KillType = KillType.Grenade,
                RespawnTime = 4
            };

            byte[] buffer = new byte[killAction.Length];
            killAction.Write(buffer.AsSpan());

            byte[] expected = new byte[]
            {
                1,
                2,
                (byte)KillType.Grenade,
                4
            };

            output.WriteLine("Expected:");
            output.WriteLine(HexDump.Create(expected));
            output.WriteLine("Actual:");
            output.WriteLine(HexDump.Create(buffer));
            
            Assert.Equal(expected, buffer);
        }

        [Fact]
        public void Test_Write_ExistingPlayer()
        {
            var existingPlayer = new ExistingPlayer
            {
                PlayerId = 1,
                Team = TeamType.Blue,
                Weapon = WeaponType.Smg,
                HeldItem = 3,
                Kills = 10,
                Color = Color.Crimson,
                Name = "Deuce"
            };

            byte[] buffer = new byte[existingPlayer.Length];
            existingPlayer.Write(buffer.AsSpan());

            byte[] expected = new byte[]
            {
                1,
                0,
                1,
                3,
                // Kills
                0xA, 0x0, 0x0, 0x0,
                // Color
                0x3C, 0x14, 0xDC,
                // Name
                0x44, 0x65, 0x75, 0x63, 0x65,
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0
            };

            output.WriteLine("Expected:");
            output.WriteLine(HexDump.Create(expected));
            output.WriteLine("Actual:");
            output.WriteLine(HexDump.Create(buffer));
            
            Assert.Equal(expected, buffer);
        }

        [Fact]
        public void Test_Write_ProgressBar()
        {
            var progressBar = new ProgressBar
            {
                EntityId = 1,
                CapturingTeam = TeamType.Green,
                Rate = 2,
                Progress = 0.6f
            };

            byte[] buffer = new byte[progressBar.Length];
            progressBar.Write(buffer.AsSpan());

            byte[] expected = new byte[]
            {
                1,
                (byte)(TeamType.Green),
                2,
                0x9A, 0x99, 0x19, 0x3F
            };

            output.WriteLine("Expected:");
            output.WriteLine(HexDump.Create(expected));
            output.WriteLine("Actual:");
            output.WriteLine(HexDump.Create(buffer));
            
            Assert.Equal(expected, buffer);
        }
    }
}