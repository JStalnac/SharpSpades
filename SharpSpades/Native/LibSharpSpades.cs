using System;
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
        public static extern unsafe void move_player(IntPtr map, NativePlayer* player, float delta, float time);
    }
}