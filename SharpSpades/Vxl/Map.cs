using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;
using SharpSpades.Native;
using System;
using System.IO;

namespace SharpSpades.Vxl
{
    public class Map
    {
        internal IntPtr NativeHandle { get; }
        internal ReadOnlyMemory<byte> RawData { get; private set; }

        private Map(IntPtr nativeHandle)
        {
            NativeHandle = nativeHandle;
        }

        internal void Free()
        {
            LibVxl.destroy_map(NativeHandle);
        }

        public static Map Load(string path)
        {
            return Load(new FileStream(path, FileMode.Open, FileAccess.Read));
        }

        public unsafe static Map Load(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("The stream must a readable stream", nameof(stream));

            var map = CreateMap();

            var ms = new MemoryStream();
            stream.CopyTo(ms);
            byte[] data = ms.ToArray();

            LibVxl.libvxl_create(map.NativeHandle, 512, 512, 64, data, data.Length);
            
            // Temporary
            byte[] outBuffer = new byte[1024 * 1024 * 64];
            int size = 0;
            
            fixed (void* ptr = outBuffer)
            {
                LibVxl.libvxl_write(map.NativeHandle, ptr, ref size);
            }

            using (ms = new MemoryStream())
            {
                using (var zlib = new ZlibStream(ms, CompressionMode.Compress, CompressionLevel.BestCompression))
                    zlib.Write(outBuffer, 0, size);
                map.RawData = ms.ToArray().AsMemory();
            }
            
            return map;
        }

        private static Map CreateMap()
        {
            var p = LibVxl.create_map();
            return new Map(p);
        }
    }
}