using System.Numerics;
using System.Runtime.InteropServices;

namespace SharpSpades.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeVector
    {
        public float X;
        public float Y;
        public float Z;

        public static Vector3 ToVector3(NativeVector vector)
            => new Vector3(vector.X, vector.Y, vector.Z);

        public static NativeVector FromVector3(Vector3 vector)
            => new NativeVector
            {
                X = vector.X,
                Y = vector.Y,
                Z = vector.Z
            };
    }
}