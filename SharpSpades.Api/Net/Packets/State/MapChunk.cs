using System;
using System.IO;

namespace SharpSpades.Api.Net.Packets.State
{
    public sealed class MapChunk : IPacket
    {
        public byte Id => 19;

        public ReadOnlyMemory<byte> MapData { get; init; }

        public MapChunk() { }

        public MapChunk(ReadOnlyMemory<byte> mapData)
        {
            MapData = mapData;
        }

        public void Read(MemoryStream ms)
            => throw new NotImplementedException();

        public void WriteTo(MemoryStream ms)
        {
            ms.Write(MapData.Span);
        }
    }
}
