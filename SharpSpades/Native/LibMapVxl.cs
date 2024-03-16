using System.Runtime.InteropServices;

namespace SharpSpades.Native
{
    internal static class LibMapVxl
    {
        private const string LibraryName = "libsharpspades";
        private const CallingConvention callingConvention = CallingConvention.Cdecl;

        [DllImport(LibraryName, CallingConvention = callingConvention)]
        public static extern IntPtr create_map();

        [DllImport(LibraryName, CallingConvention = callingConvention)]
        public static extern void mapvxl_readmap(IntPtr map, byte[] data);

        [DllImport(LibraryName, CallingConvention = callingConvention)]
        public unsafe static extern int mapvxl_writemap(IntPtr map, void* ptr);

        [DllImport(LibraryName, CallingConvention = callingConvention)]
        public static extern void destroy_map(IntPtr map);
    }
}