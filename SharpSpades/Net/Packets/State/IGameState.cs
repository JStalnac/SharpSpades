using System;

namespace SharpSpades.Net.Packets.State
{
    public interface IGameState
    {
        public void WriteTo(Span<byte> ms);
        public int Length { get; }
    }
}
