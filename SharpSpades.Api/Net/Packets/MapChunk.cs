namespace SharpSpades.Api.Net.Packets
{
    public struct MapChunk : IPacket
    {
        public byte Id => 19;

        public int Length => MapData.Length;

        public ReadOnlyMemory<byte> MapData { get; init; }

        public MapChunk(ReadOnlyMemory<byte> mapData)
        {
            MapData = mapData;
        }

        public void Read(ReadOnlySpan<byte> buffer) { }

        public void Write(Span<byte> buffer)
            => MapData.Span.CopyTo(buffer);
    }
}