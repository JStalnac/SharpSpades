using SharpSpades.Api.Utils;
using System;
using System.Drawing;
using System.IO;

namespace SharpSpades.Api.Net.Packets.State
{
    public sealed class StateData : IPacket
    {
        public byte Id => 15;

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

                // We need space for the null byte
                if (value.Length >= 10)
                    throw new ArgumentOutOfRangeException("The team name can a maximum of nine characters long");

                blueName = value;
            }
        }
        public string GreenName
        {
            get => greenName;
            init
            {
                Throw.IfNull(value, nameof(value));

                // We need space for the null byte
                if (value.Length >= 10)
                    throw new ArgumentOutOfRangeException("The team name can a maximum of nine characters long");

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

        public void Read(MemoryStream ms)
            => throw new NotImplementedException();

        public void WriteTo(MemoryStream ms)
        {
            if (State is null)
                throw new InvalidOperationException("State cannot be null");

            ms.WriteByte(PlayerId);
            ms.WriteColor(FogColor);
            ms.WriteColor(BlueColor);
            ms.WriteColor(GreenColor);

            if (State is CtfState ctf)
            {
                ctf.WriteTo(ms);
                ms.WriteByte(0);
            }
            else if (State is TcState tc)
            {
                tc.WriteTo(ms);
                ms.WriteByte(1);
            }
            else
            {
                throw new InvalidOperationException("This was not supposed to happen :(");
            }
        }
    }
}
