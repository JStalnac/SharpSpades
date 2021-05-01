using SharpSpades.Utils;
using System;
using System.Numerics;

namespace SharpSpades.Net.Packets.State
{
    public sealed class CtfState : IGameState
    {
        public int Length => 52;

        public byte BlueScore { get; init; }
        public byte GreenScore { get; init; }
        public byte CaptureLimit { get; init; }

        public bool BlueHasIntel { get; init; }
        public bool GreenHasIntel { get; init; }

        public IntelLocation BlueIntel { get; init; }
        public IntelLocation GreenIntel { get; init; }

        public Vector3 BlueBasePosition { get; init; }
        public Vector3 GreenBasePosition { get; init; }

        public void WriteTo(Span<byte> buffer)
        {
            buffer[0] = BlueScore;
            buffer[1] = GreenScore;
            buffer[2] = CaptureLimit;

            byte team1HasIntel = (byte)(BlueHasIntel ? 1 : 0);
            byte team2HasIntel = (byte)(GreenHasIntel ? 1 : 0);

            byte intelFlags = (byte)(team1HasIntel | (team2HasIntel << 1));
            buffer[3] = intelFlags;

            BlueIntel.Write(buffer.Slice(4, 12));
            GreenIntel.Write(buffer.Slice(16, 12));

            buffer.WritePosition(BlueBasePosition, 28);
            buffer.WritePosition(GreenBasePosition, 40);
        }
    }

    public struct IntelLocation
    {
        /// <summary>
        /// The id of the player who holds the intel.
        /// </summary>
        public byte Holder { get; init; }

        public bool IsHeld { get; init; }

        public Vector3 Position { get; init; }

        internal void Write(Span<byte> buffer)
        {
            if (IsHeld)
            {
                buffer[0] = Holder;
                buffer.Slice(1).Fill(0);
            }
            else
            {
                buffer.WritePosition(Position);
            }
        }
    }
}
