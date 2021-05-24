using SharpSpades.Net.Packets.Attributes;
using System.Threading.Tasks;

namespace SharpSpades.Net.Packets.State
{
    [WriteOnly]
    public sealed partial class MapStart : Packet
    {
        public override byte Id => 18;
        
        public override int Length => sizeof(uint);

        [Field(0)]
        public uint MapSize { get; set; }

        public MapStart() { }

        public MapStart(uint mapSize) => this.MapSize = mapSize;

        internal override Task HandleAsync(Client client) => Task.CompletedTask;
    }
}
