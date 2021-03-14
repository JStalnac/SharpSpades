using SharpSpades.Api.Utils;
using System.IO;
using System.Numerics;

namespace SharpSpades.Api.Net.Packets.State
{
    public sealed class CtfState : IGameState
    {
        public byte BlueScore { get; init; }
        public byte GreenScore { get; init; }
        public byte CaptureLimit { get; init; }

        public bool BlueHasIntel { get; init; }
        public bool GreenHasIntel { get; init; }

        public IntelLocation BlueIntel { get; init; }
        public IntelLocation GreenIntel { get; init; }

        public Vector3 BlueBasePosition { get; init; }
        public Vector3 GreenBasePosition { get; init; }

        public void WriteTo(MemoryStream ms)
        {
            ms.WriteByte(BlueScore);
            ms.WriteByte(GreenScore);
            ms.WriteByte(CaptureLimit);

            byte team1HasIntel = BlueHasIntel ? 1 : 0;
            byte team2HasIntel = GreenHasIntel ? 1 : 0;

            byte intelFlags = (byte)(team1HasIntel | (team2HasIntel << 1));
            ms.WriteByte(intelFlags);

            BlueIntel.Write(ms);
            GreenIntel.Write(ms);

            ms.WritePositionLittleEndian(BlueBasePosition);
            ms.WritePositionLittleEndian(GreenBasePosition);
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

        internal void Write(MemoryStream ms)
        {
            if (IsHeld)
            {
                ms.WriteByte(Holder);
                ms.Write(new byte[11]);
            }
            else
            {
                ms.WritePositionLittleEndian(Position);
            }
        }
    }
}
