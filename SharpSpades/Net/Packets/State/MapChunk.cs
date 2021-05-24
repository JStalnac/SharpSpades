using System;
using System.Threading.Tasks;

namespace SharpSpades.Net.Packets.State
{
    public sealed class MapChunk : Packet
    {
        public override byte Id => 19;

        public override int Length => MapData.Length;

        public ReadOnlyMemory<byte> MapData { get; init; }

        public MapChunk() { }

        public MapChunk(ReadOnlyMemory<byte> mapData) => this.MapData = mapData;

        internal override void Read(ReadOnlySpan<byte> buffer)
            => throw new NotImplementedException();

        internal override void Write(Span<byte> buffer) => this.MapData.Span.CopyTo(buffer);

        internal override Task HandleAsync(Client client) => Task.CompletedTask;
    }
}
