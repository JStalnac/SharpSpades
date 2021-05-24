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
            buffer[0] = this.BlueScore;
            buffer[1] = this.GreenScore;
            buffer[2] = this.CaptureLimit;
            buffer[3] = (byte)((byte)(this.BlueHasIntel ? 1 : 0) | ((byte)(this.GreenHasIntel ? 1 : 0) << 1));

            this.BlueIntel.Write(buffer.Slice(4, 12));
            this.GreenIntel.Write(buffer.Slice(16, 12));

            buffer.WritePosition(this.BlueBasePosition, 28);
            buffer.WritePosition(this.GreenBasePosition, 40);
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
            if (this.IsHeld)
            {
                buffer[0] = this.Holder;
                buffer[1..].Fill(0);
            }
            else
                buffer.WritePosition(this.Position);
        }
    }
}
