using SharpSpades.Api.Utils;
using System;
using System.Drawing;

namespace SharpSpades.Api.Net.Packets.State
{
    public sealed class StateData : IPacket
    {
        public byte Id => 15;

        // Player Id, Gamemode Id, 3 * Color, 2 * Name
        public int Length => 1 + 1 + 3 * 3 + 2 * 10 + (int)State?.Length;

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

        private string blueName = "Blue";
        private string greenName = "Green";

        public string BlueName
        {
            get => blueName;
            init
            {
                Throw.IfNull(value, nameof(value));

                if (value.Length > 10)
                    throw new ArgumentOutOfRangeException("The team name can a maximum of ten characters long");

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
                    throw new ArgumentOutOfRangeException("The team name can a maximum of ten characters long");

                greenName = value;
            }
        }

        public IGameState State { get; init; }

        public StateData() { }

        public StateData(byte playerId, Color fogColor, Color blueTeamColor,
            Color greenTeamColor, string blueName, string greenName)
        {
            PlayerId = playerId;
            FogColor = fogColor;
            BlueColor = blueTeamColor;
            GreenColor = greenTeamColor;
            BlueName = blueName;
            GreenName = greenName;
        }

        public void Read(ReadOnlySpan<byte> buffer)
            => throw new NotImplementedException();

        public void WriteTo(Span<byte> buffer)
        {
            if (State is null)
                throw new InvalidOperationException("State cannot be null");

            buffer[0] = PlayerId;
            buffer.WriteColor(FogColor, 1);
            buffer.WriteColor(BlueColor, 4);
            buffer.WriteColor(GreenColor, 7);

            Span<byte> name = StringUtils.ToCP437String(BlueName);
            name.CopyTo(buffer.Slice(10, 10));
            name = StringUtils.ToCP437String(GreenName);
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
            {
                throw new InvalidOperationException("This was not supposed to happen :(");
            }
        }
    }
}
