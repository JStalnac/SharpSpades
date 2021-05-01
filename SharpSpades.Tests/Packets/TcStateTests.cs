using NUnit.Framework;
using SharpSpades.Net.Packets.State;
using SharpSpades.Utils;
using System;
using System.Collections.Immutable;
using System.Numerics;

namespace SharpSpades.Tests.Packets
{
    public class TcStateTests
    {
        [Test]
        public void Test_Write()
        {
            var packet = new TcState
            {
                Territories = new Territory[]
                {
                    new Territory
                    {
                        State = TerritoryState.Blue,
                        Position = new Vector3(1, 1, 1)
                    },
                    new Territory
                    {
                        State = TerritoryState.Neutral,
                        Position = new Vector3(2, 2, 2)
                    },
                    new Territory
                    {
                        State = TerritoryState.Green,
                        Position = new Vector3(3, 3, 3)
                    },
                    new Territory
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
                0x00, 0x00, 0x80, 0x3F,
                0x00, 0x00, 0x80, 0x3F,
                0x00, 0x00, 0x80, 0x3F,
                // State
                0x01,
                //// Territory 2
                // Position
                0x00, 0x00, 0x00, 0x40,
                0x00, 0x00, 0x00, 0x40,
                0x00, 0x00, 0x00, 0x40,
                // State
                0x00,
                //// Territory 3
                // Position
                0x00, 0x00, 0x40, 0x40,
                0x00, 0x00, 0x40, 0x40,
                0x00, 0x00, 0x40, 0x40,
                // State
                0x02,
                //// Territory 4
                // Position
                0x00, 0x00, 0x80, 0x40,
                0x00, 0x00, 0x80, 0x40,
                0x00, 0x00, 0x80, 0x40,
                // State
                0x02
            };

            Console.WriteLine(HexDump.Create(expected));
            Console.WriteLine(HexDump.Create(buffer));

            Assert.AreEqual(expected, buffer.ToArray());
        }
    }
}
