using System;

namespace SharpSpades.Api.Net.Packets.State
{
    public interface IGameState
    {
        public void WriteTo(Span<byte> ms);
        public int Length { get; }
    }
}
