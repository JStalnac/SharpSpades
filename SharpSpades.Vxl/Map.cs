using System;
using System.Buffers.Binary;
using System.Collections;
using System.Drawing;
using System.IO;

namespace SharpSpades.Vxl
{
    public class Map
    {
        public const int MapX = 512;
        public const int MapY = 512;
        public const int MapZ = 64;
        public const int DefaultColor = unchecked((int)0xFF674028);

        // One bit per block
        private readonly BitArray geometry;

        // Five bytes per block
        private readonly int?[] colors;
        
        public Map()
        {
            geometry = new(MapX * MapY * MapZ, true);
            colors = new int?[MapX * MapY * MapZ];
        }
        
        public static Map Load(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("The stream must a readable stream", nameof(stream));

            var map = new Map();

            for (int y = 0; y < MapY; y++)
                for (int x = 0; x < MapX; x++)
                    ReadColumn(stream, map, x, y);
            return map;
        }

        internal static void ReadColumn(Stream stream, Map map, int x, int y)
        {
            int z = 0;
            VxlSpan span = null;
            while (true)
            {
                // TODO: Error on invalid span

                // First span of the column
                if (span is null)
                    span = ReadSpan(stream);

                // Set air
                for (; z < span.ColorStart; z++)
                    map.geometry.Set(GetIndex(x, y, z), false);

                // Set top color
                Span<byte> colorData = span.Colors;
                for (z = span.ColorStart; z < span.ColorEnd; z++)
                {
                    map.colors[GetIndex(x, y, z)] = BinaryPrimitives.ReadInt32LittleEndian(colorData.Slice(0, 4));
                    colorData = colorData.Slice(4);
                }

                // Last span of the column
                if (span.Length == 0)
                    break;

                VxlSpan nextSpan = ReadSpan(stream);

                int topColorLength = span.ColorEnd - span.ColorStart + 1;
                int bottomColorLength = span.Length - 1 - topColorLength;

                // Bottom color run
                for (z = nextSpan.AirStart - bottomColorLength; z < nextSpan.AirStart; z++)
                {
                    map.colors[GetIndex(x, y, z)] = BinaryPrimitives.ReadInt32LittleEndian(colorData.Slice(0, 4));
                    colorData = colorData.Slice(4);
                }

                span = nextSpan;
            }
        }

        private static VxlSpan ReadSpan(Stream stream)
        {
            // Read the length of the span
            byte length = (byte)stream.ReadByte();

            byte colorStart = (byte)stream.ReadByte();
            byte colorEnd = (byte)stream.ReadByte();
            byte airStart = (byte)stream.ReadByte();

            // Calculate how much data we have
            int colorDataLength;
            if (length == 0)
                colorDataLength = 4 * (colorEnd - colorStart + 1);
            else
            {
                // Remove one from the length because it includes the span
                // header and we already read it.
                colorDataLength = (length - 1) * 4;
            }
            
            // Read data
            byte[] colors = new byte[colorDataLength];
            stream.Read(colors, 0, colors.Length);

            return new VxlSpan(length, airStart, colorStart, colorEnd, colors);
        }

        public static int GetIndex(int x, int y, int z)
        {
            if (!IsInRange(x, y, z))
                throw new ArgumentOutOfRangeException($"The supplied position was out of range (X: {x} Y: {y} Z: {z})");
            return x + (y * MapX) + (z * MapX * MapY);
        }

        public static bool IsInRange(int x, int y, int z)
            => x >= 0 && x < MapX
            && y >= 0 && y < MapY
            && z >= 0 && z < MapZ;

        public void SetColor(int x, int y, int z, Color color)
        {
            // TODO: Test
            Span<byte> span = stackalloc byte[]
            {
                color.R, color.G, color.B, color.A
            };
            SetColor(x, y, z, BinaryPrimitives.ReadInt32LittleEndian(span));
        }

        public void SetColor(int x, int y, int z, int color)
        {
            int index = GetIndex(x, y, z);
            geometry[index] = true;
            colors[index] = color;
        }

        public void SetAir(int x, int y, int z)
            => geometry[GetIndex(x, y, z)] = false;

        public Color GetColor(int x, int y, int z)
        {
            int index = GetIndex(x, y, z);
            
            if (!geometry[index])
                return Color.Empty;

            int? color = colors[index];
            return Color.FromArgb(color.HasValue ? color.Value : DefaultColor);
        }

        public int GetRawColor(int x, int y, int z)
        {
            int? color = colors[GetIndex(x, y, z)];
            return color.HasValue ? color.Value : DefaultColor;
        }

        public bool IsSolid(int x, int y, int z)
            => geometry[GetIndex(x, y, z)];

        public bool IsSurface(int x, int y, int z)
        {
            if (!IsInRange(x, y, z))
                throw new ArgumentOutOfRangeException($"The supplied position was out of range");
            
            // If the block is solid and at least one of its neighbors is an air block
            return IsSolid(x, y, z)
                && ((x > 0 && !IsSolid(x - 1, y, z))
                    || (x + 1 < MapX && !IsSolid(x + 1, y, z))
                    || (y > 0 && !IsSolid(x, y - 1, z))
                    || (y + 1 < MapY) && !IsSolid(x, y + 1, z))
                    || (z > 0 && !IsSolid(x, y, z - 1))
                    || (z + 1 < MapZ && !IsSolid(x, y, z + 1));
        }
    }
}
