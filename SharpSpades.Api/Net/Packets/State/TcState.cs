using SharpSpades.Api.Utils;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Numerics;

namespace SharpSpades.Api.Net.Packets.State
{
    public sealed class TcState : IGameState
    {
        const int MaxTerritories = 16;

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

        public void WriteTo(MemoryStream ms)
        {
            foreach (var t in territories)
            {
                ms.WritePositionLittleEndian(t.Position);
                ms.WriteByte((byte)t.State);
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
