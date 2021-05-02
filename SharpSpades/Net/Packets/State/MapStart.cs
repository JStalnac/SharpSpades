using SharpSpades.Utils;
using System;
using System.Threading.Tasks;

namespace SharpSpades.Net.Packets.State
{
    public sealed class MapStart : Packet
    {
        public override byte Id => 18;
        
        public override int Length => sizeof(uint);

        public uint MapSize { get; init; }

        public MapStart() { }

        public MapStart(uint mapSize)
        {
            MapSize = mapSize;
        }

        internal override void Read(ReadOnlySpan<byte> buffer)
            => throw new NotImplementedException();

        internal override void WriteTo(Span<byte> buffer)
        {
            buffer.WriteUInt32LittleEndian(MapSize);
        }

        internal override Task HandleAsync(Client client)
            => Task.CompletedTask;
    }
}
