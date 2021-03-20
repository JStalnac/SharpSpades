using SharpSpades.Api.Utils;
using System;
using System.Collections.Immutable;
using System.Numerics;

namespace SharpSpades.Api.Net.Packets.State
{
    public sealed class TcState : IGameState
    {
        const int MaxTerritories = 16;

        public int Length => 1 + territories.Length * 13;

        private ImmutableArray<Territory> territories = ImmutableArray<Territory>.Empty;

        public ImmutableArray<Territory> Territories
        {
            get => territories;
            init
            {
                if (value.Length > MaxTerritories)
                    throw new ArgumentOutOfRangeException("Maximum number of territories is 16");

                territories = value;
            }
        }

        public void WriteTo(Span<byte> buffer)
        {
            buffer[0] = (byte)Territories.Length;
            Span<byte> span = buffer.Slice(1);
            foreach (var t in territories)
            {
                span.WritePosition(t.Position);
                span[12] = (byte)t.State;
                span = span.Slice(13);
            }
        }
    }

    public class Territory
    {
        public TerritoryState State { get; init; }

        public Vector3 Position { get; init; }
    }

    public enum TerritoryState
    {
        Neutral,
        Blue,
        Green
    }
}
