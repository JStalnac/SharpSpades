using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;
using SharpSpades.Api.Vxl;
using SharpSpades.Native;
using System.Numerics;

namespace SharpSpades.Vxl
{
    public class Map : IMap
    {
        internal IntPtr NativeHandle { get; }
        internal ReadOnlyMemory<byte> RawData { get; private set; }

        private Map(IntPtr nativeHandle)
        {
            NativeHandle = nativeHandle;
        }

        public bool CastRay(Vector3 position, Vector3 orientation, float length, out Vector3? hit)
        {
            if (LibSharpSpades.cast_ray(NativeHandle, position.X, position.Y, position.Z,
                    orientation.X, orientation.Y, orientation.Z, length,
                    out long x, out long y, out long z) == 1)
            {
                hit = new Vector3(x, y, z);
                return true;
            }
            hit = null;
            return false;
        }

        internal void Free()
        {
            LibMapVxl.destroy_map(NativeHandle);
        }

        public static async Task<Map> LoadAsync(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            return await LoadAsync(fs);
        }

        public static async Task<Map> LoadAsync(Stream stream)
        {
            // Mostly synchronous
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("The stream must a readable stream", nameof(stream));

            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return Load(ms.ToArray());
        }

        public unsafe static Map Load(byte[] data)
        {
            var map = CreateMap();
            LibMapVxl.mapvxl_readmap(map.NativeHandle, data);
            
            // TODO: Move somewhere else
            byte[] outBuffer = new byte[1024 * 1024 * 10];
            
            fixed (void* ptr = outBuffer)
            {
                LibMapVxl.mapvxl_writemap(map.NativeHandle, ptr);
            }

            var ms = new MemoryStream();
            using (var zlib = new ZlibStream(ms, CompressionMode.Compress, CompressionLevel.BestCompression))
                zlib.Write(outBuffer.AsSpan());
            map.RawData = ms.ToArray().AsMemory();
            
            return map;
        }

        private static Map CreateMap()
        {
            var p = LibMapVxl.create_map();
            return new Map(p);
        }
    }
}