using System.Runtime.InteropServices;

namespace SharpSpades.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativePlayer
    {
        // p
        public NativeVector Position;
        // e
        public NativeVector Position2;
        // v
        public NativeVector Velocity;
        public NativeVector s;
        public NativeVector h;
        // f
        public NativeVector Orientation;

        public int Forward, Backward, Left, Right, Jump, Crouch, Sneak, Sprint, PrimaryFire,
            SecondaryFire;
        public float LastClimb;
        public int Airborne, Wade, Alive, Weapon;
    }
}