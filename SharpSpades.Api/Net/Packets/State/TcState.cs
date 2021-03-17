using SharpSpades.Api.Utils;
using System;
using System.Collections.Immutable;
using System.Numerics;

namespace SharpSpades.Api.Net.Packets.State
{
    public sealed class TcState : IGameState
    {
        const int MaxTerritories = 16;

        public int Length => territories.Length * 4;

        private ImmutableArray<Territory> territories;

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
            Span<byte> span = buffer;
            foreach (var t in territories)
            {
                span.WritePosition(t.Position);
                span[3] = (byte)t.State;
                span = span.Slice(3);
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
