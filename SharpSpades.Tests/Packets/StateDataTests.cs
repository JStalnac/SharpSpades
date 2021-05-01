using NUnit.Framework;
using SharpSpades.Net.Packets.State;
using SharpSpades.Utils;
using System;
using System.Drawing;

namespace SharpSpades.Tests.Packets
{
    public class StateDataTests
    {
        [Test]
        public void Test_Set_BlueName()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new StateData
                {
                    BlueName = "This text is longer than 10 characters"
                };
            });
        }

        [Test]
        public void Test_Set_GreenName()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new StateData
                {
                    GreenName = "This text is longer than 10 characters"
                };
            });
        }

        [Test]
        public void Test_Write()
        {
            var packet = new StateData
            {
                PlayerId = 1,
                FogColor = Color.Red,
                BlueColor = Color.Blue,
                GreenColor = Color.Green,
                BlueName = "AAAAAAAAAA",
                GreenName = "BBBBBBBBBB",
                State = new TcState()
            };
            Span<byte> buffer = new byte[packet.Length];
            buffer.Fill(0x1);
            packet.WriteTo(buffer);

            byte[] expected = new byte[]
            {
                // Player ID
                0x01,
                // Fog
                0x00, 0x00, 0xFF,
                // Blue team
                0xFF, 0x00, 0x00,
                // Green team
                0x00, 0x80, 0x00,
                // Blue name
                0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41,
                // Green name
                0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42,
                // Gamemode ID
                0x01,
                0x00
                //#region CTF State
                //// Scores
                //0x00, 0x00, 0x00,
                //// Intel flags
                //0x00,
                //// Blue intel location
                //0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                //// Green intel location 
                //0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                //// Blue base location
                //0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                //// Green base location
                //0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                //#endregion
            };

            Console.WriteLine(HexDump.Create(expected));
            Console.WriteLine(HexDump.Create(buffer));

            Assert.AreEqual(expected, buffer.ToArray());
        }
    }
}