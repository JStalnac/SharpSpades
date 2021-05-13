using SharpSpades.Net.Packets.State;
using System;
using System.Numerics;
using Xunit;

namespace SharpSpades.Tests.Packets
{
    public class CtfStateTests
    {
        [Fact]
        public void Test_Write()
        {
            var packet = new CtfState
            {
                BlueScore = 2,
                GreenScore = 3,
                CaptureLimit = 5,
                BlueHasIntel = true,
                GreenHasIntel = true,
                BlueIntel = new IntelLocation
                {
                    Holder = 1,
                    IsHeld = true
                },
                GreenIntel = new IntelLocation
                {
                    IsHeld = false,
                    Position = new Vector3(1, 1, 1)
                },
                BlueBasePosition = new Vector3(10, 10, 10),
                GreenBasePosition = new Vector3(20, 20, 20)
            };
            Span<byte> buffer = new byte[packet.Length];
            buffer.Fill(0x1);
            packet.WriteTo(buffer);

            byte[] expected = new byte[]
            {
                // Scores
                0x02,
                0x03,
                // Capture limit
                0x05,
                // Intel flags
                0x03,
                // Blue intel location
                // Holder
                0x01,
                // Padding
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // Green intel location
                // X
                0x00, 0x00, 0x80, 0x3F,
                // Y
                0x00, 0x00, 0x80, 0x3F,
                // Z
                0x00, 0x00, 0x80, 0x3F,
                // Blue base location
                // X
                0x00, 0x00, 0x20, 0x41,
                // Y
                0x00, 0x00, 0x20, 0x41,
                // Z
                0x00, 0x00, 0x20, 0x41,
                // Green base location
                // X
                0x00, 0x00, 0xA0, 0x41,
                // Y
                0x00, 0x00, 0xA0, 0x41,
                // Z
                0x00, 0x00, 0xA0, 0x41,
            };

            // Console.WriteLine(HexDump.Create(expected));
            // Console.WriteLine(HexDump.Create(buffer));

            Assert.Equal(expected, buffer.ToArray());
        }
    }
}
