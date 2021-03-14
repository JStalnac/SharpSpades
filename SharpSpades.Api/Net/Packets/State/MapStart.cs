using SharpSpades.Api.Utils;
using System;
using System.IO;

namespace SharpSpades.Api.Net.Packets.State
{
    public sealed class MapStart : IPacket
    {
        public byte Id => 18;

        public uint MapSize { get; init; }

        public MapStart() { }

        public MapStart(uint mapSize)
        {
            MapSize = mapSize;
        }

        public void Read(MemoryStream ms)
            => throw new NotImplementedException();

        public void WriteTo(MemoryStream ms)
        {
            ms.WriteUInt32LittleEndian(MapSize);
        }
    }
}
