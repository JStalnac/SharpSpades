using SharpSpades.Vxl;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SharpSpades.Native
{
    internal static class LibSharpSpades
    {
        private const string LibraryName = "libsharpspades";
        private const CallingConvention callingConvention = CallingConvention.Cdecl;

        [DllImport(LibraryName, CallingConvention = callingConvention)]
        public static extern unsafe NativePlayer* create_player();

        [DllImport(LibraryName, CallingConvention = callingConvention)]
        public static extern unsafe void destroy_player(NativePlayer* player);

        [DllImport(LibraryName, CallingConvention = callingConvention)]
        public static extern unsafe void move_player(IntPtr map, NativePlayer* player, float time, float delta);

        public static bool ValidateHit(Vector3 position, Vector3 orientation, Vector3 target, float tolerance)
        {
            return validate_hit(NativeVector.FromVector3(position),
                NativeVector.FromVector3(orientation),
                NativeVector.FromVector3(target), tolerance) == 0;
        }

        [DllImport(LibraryName, CallingConvention = callingConvention)]
        public static extern int validate_hit(NativeVector position, NativeVector orientation,
                NativeVector target, float tolerance);

        [DllImport(LibraryName, CallingConvention = callingConvention)]
        public static extern int cast_ray(IntPtr map, float px, float py, float pz,
                float ox, float oy, float oz, float length, out long x, out long y, out long z);
    }
}