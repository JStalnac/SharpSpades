using SharpSpades.Api.Net.Packets;
using SharpSpades.Utils;
using System.Collections.Immutable;
using System.Numerics;
using Xunit;
using Xunit.Abstractions;

namespace SharpSpades.Tests.Net.Packets
{
    public class TcStateTests
    {
        private readonly ITestOutputHelper output;

        public TcStateTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Test_Write()
        {
            var packet = new TcState
            {
                Territories = new Territory[]
                {
                    new()
                    {
                        State = TerritoryState.Blue,
                        Position = new Vector3(1, 1, 1)
                    },
                    new()
                    {
                        State = TerritoryState.Neutral,
                        Position = new Vector3(2, 2, 2)
                    },
                    new()
                    {
                        State = TerritoryState.Green,
                        Position = new Vector3(3, 3, 3)
                    },
                    new()
                    {
                        State = TerritoryState.Green,
                        Position = new Vector3(4, 4, 4)
                    }
                }.ToImmutableArray()
            };

            Span<byte> buffer = new byte[packet.Length];
            buffer.Fill(0x1);
            packet.WriteTo(buffer);

            byte[] expected = new byte[]
            {
                // Length
                0x04,
                //// Territory 1
                // Position
                0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F,

                // State
                0x01,
                //// Territory 2
                // Position
                0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40,

                // State
                0x00,
                //// Territory 3
                // Position
                0x00, 0x00, 0x40, 0x40, 0x00, 0x00, 0x40, 0x40, 0x00, 0x00, 0x40, 0x40,

                // State
                0x02,
                //// Territory 4
                // Position
                0x00, 0x00, 0x80, 0x40, 0x00, 0x00, 0x80, 0x40, 0x00, 0x00, 0x80, 0x40,

                // State
                0x02
            };

            output.WriteLine("Expected:");
            output.WriteLine(HexDump.Create(expected));
            output.WriteLine("Actual:");
            output.WriteLine(HexDump.Create(buffer));

            Assert.Equal(expected, buffer.ToArray());
        }
    }
}