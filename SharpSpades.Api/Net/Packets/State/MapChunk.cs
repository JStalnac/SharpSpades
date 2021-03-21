using System;
using System.Threading.Tasks;

namespace SharpSpades.Api.Net.Packets.State
{
    public sealed class MapChunk : IPacket
    {
        public byte Id => 19;

        public ReadOnlyMemory<byte> MapData { get; init; }

        public int Length => MapData.Length;

        public MapChunk() { }

        public MapChunk(ReadOnlyMemory<byte> mapData)
        {
            MapData = mapData;
        }

        public void Read(ReadOnlySpan<byte> buffer)
            => throw new NotImplementedException();

        public void WriteTo(Span<byte> buffer)
        {
            MapData.Span.CopyTo(buffer);
        }

        public Task HandleAsync(IClient client)
            => Task.CompletedTask;
    }
}
