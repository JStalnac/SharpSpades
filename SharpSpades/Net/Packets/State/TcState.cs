using SharpSpades.Utils;
using System;
using System.Collections.Immutable;
using System.Numerics;

namespace SharpSpades.Net.Packets.State
{
    public sealed class TcState : IGameState
    {
        private const int MaxTerritories = 16;

        public int Length => 1 + (territories.Length * 13);

        private ImmutableArray<Territory> territories = ImmutableArray<Territory>.Empty;

        public ImmutableArray<Territory> Territories
        {
            get => territories;
            init
            {
                Throw.If(value.Length, x => x > MaxTerritories, new ArgumentOutOfRangeException(nameof(value.Length), "Maximum number of territories is 16"));

                territories = value;
            }
        }

        public void WriteTo(Span<byte> buffer)
        {
            buffer[0] = (byte)this.Territories.Length;
            
            Span<byte> span = buffer[1..];

            foreach (var t in territories)
            {
                span.WritePosition(t.Position);
                span[12] = (byte)t.State;
                span = span[13..];
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
