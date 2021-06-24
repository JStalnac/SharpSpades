using System;
using System.Runtime.InteropServices;

namespace SharpSpades.Native
{
    internal static class LibVxl
    {
        private const string LibraryName = "libsharpspades";
        private const CallingConvention callingConvention = CallingConvention.Cdecl;

        [DllImport(LibraryName, CallingConvention = callingConvention)]
        public static extern IntPtr create_map();

        [DllImport(LibraryName, CallingConvention = callingConvention)]
        public static extern void libvxl_create(IntPtr map, int width, int height, int depth, byte[] data, int size);

        [DllImport(LibraryName, CallingConvention = callingConvention)]
        public unsafe static extern void libvxl_write(IntPtr map, void* ptr, ref int size);

        [DllImport(LibraryName, CallingConvention = callingConvention)]
        public static extern void destroy_map(IntPtr map);
    }
}