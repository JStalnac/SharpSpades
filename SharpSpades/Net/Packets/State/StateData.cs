﻿using SharpSpades.Utils;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace SharpSpades.Net.Packets.State
{
    public sealed class StateData : Packet
    {
        public override byte Id => 15;

        // Player Id, Gamemode Id, 3 * Color, 2 * Name
        public override int Length => 1 + 1 + 3 * 3 + 2 * 10 + (int)State?.Length;

        /// <summary>
        /// The id of the player.
        /// </summary>
        public byte PlayerId { get; init; }

        public Color FogColor { get; init; }

        /// <summary>
        /// Color of the blue team.
        /// </summary>
        public Color BlueColor { get; init; }

        /// <summary>
        /// Color of the green team.
        /// </summary>
        public Color GreenColor { get; init; }

        private readonly string blueName = "Blue";
        private readonly string greenName = "Green";

        private const string TeamNameException = "The team name can a maximum of ten characters long";

        public string BlueName
        {
            get => blueName;
            init
            {
                Throw.IfNull(value, nameof(value));

                if (value.Length > 10)
                    throw new ArgumentOutOfRangeException(nameof(value), TeamNameException);

                blueName = value;
            }
        }

        public string GreenName
        {
            get => greenName;
            init
            {
                Throw.IfNull(value, nameof(value));

                if (value.Length > 10)
                    throw new ArgumentOutOfRangeException(nameof(value), TeamNameException);

                greenName = value;
            }
        }

        public IGameState State { get; init; }

        public StateData() { }

        public StateData(byte playerId,
            Color fogColor,
            Color blueTeamColor,
            Color greenTeamColor,
            string blueName,
            string greenName)
        {
            PlayerId = playerId;
            FogColor = fogColor;
            BlueColor = blueTeamColor;
            GreenColor = greenTeamColor;
            BlueName = blueName;
            GreenName = greenName;
        }

        internal override void Read(ReadOnlySpan<byte> buffer)
            => throw new NotImplementedException();

        internal override void Write(Span<byte> buffer)
        {
            Throw.IfNull(State, nameof(State));

            buffer[0] = PlayerId;
            buffer.WriteColor(FogColor, 1);
            buffer.WriteColor(BlueColor, 4);
            buffer.WriteColor(GreenColor, 7);

            Span<byte> name = BlueName.ToCP437String();
            name.CopyTo(buffer.Slice(10, 10));
            name = GreenName.ToCP437String();
            name.CopyTo(buffer.Slice(20, 10));

            if (State is CtfState ctf)
            {
                buffer[30] = 0;
                ctf.WriteTo(buffer.Slice(31));
            }
            else if (State is TcState tc)
            {
                buffer[30] = 1;
                tc.WriteTo(buffer.Slice(31));
            }
            else
                throw new InvalidOperationException("This was not supposed to happen :(");
        }

        internal override Task HandleAsync(Client client) => Task.CompletedTask;
    }
}