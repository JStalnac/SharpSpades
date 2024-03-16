using System.Runtime.InteropServices;

namespace SharpSpades.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativePlayer
    {
        public Tool Tool;
        public byte Forward;
        public byte Backward;
        public byte Left;
        public byte Right;
        public byte Jump;
        public byte Crouch;
        public byte Sneak;
        public byte Sprint;
        public byte PrimaryFire;
        public byte SecondaryFire;

        public NativeVector Position;
        public NativeVector EyePosition;
        public NativeVector Velocity;
        public NativeVector StrafeOrientation;
        public NativeVector HeightOrientation;
        // ForwardOrientation
        public NativeVector Orientation;
        public NativeVector PreviousOrientation;

        public byte Airborne;
        public byte Wade;
        public float LastClimb;
    }
}