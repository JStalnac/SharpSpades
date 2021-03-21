using SharpSpades.Api.Utils;
using System;
using System.Threading.Tasks;

namespace SharpSpades.Api.Net.Packets.State
{
    public sealed class MapStart : IPacket
    {
        public byte Id => 18;

        public uint MapSize { get; init; }

        public int Length => sizeof(uint);

        public MapStart() { }

        public MapStart(uint mapSize)
        {
            MapSize = mapSize;
        }

        public void Read(ReadOnlySpan<byte> buffer)
            => throw new NotImplementedException();

        public void WriteTo(Span<byte> buffer)
        {
            buffer.WriteUInt32LittleEndian(MapSize);
        }

        public Task HandleAsync(IClient client)
            => Task.CompletedTask;
    }
}
