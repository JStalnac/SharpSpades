using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SharpSpades.Api.Utils
{
    public static class MemoryStreamExtensions
    {
        private static readonly Encoding cp437Encoding;

        static MemoryStreamExtensions()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            cp437Encoding = Encoding.GetEncoding(437);
        }

        public static void WriteUInt32LittleEndian(this MemoryStream ms, uint i)
        {
            byte[] bytes = BitConverter.GetBytes(i);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            ms.Write(bytes, 0, bytes.Length);
        }

        public static void WriteSByte(this MemoryStream ms, sbyte b)
        {
            ms.WriteByte(unchecked((byte)b));
        }

        public static void WriteFloatLittleEndian(this MemoryStream ms, float f)
        {
            byte[] bytes = BitConverter.GetBytes(f);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            ms.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Writes a CP437 string to the memory stream. Writes an extra null byte to signal end of string.
        /// </summary>
        /// <param name="ms">Memory stream to write to.</param>
        /// <param name="s">The string to write to the memory stream.</param>
        public static void WriteCP437String(this MemoryStream ms, string s)
        {
            Throw.IfNull(s, nameof(s));

            ms.Write(cp437Encoding.GetBytes(s));
            ms.WriteByte(0);
        }

        /// <summary>
        /// Writes three floats representing the position to the memory stream as little endian.
        /// </summary>
        /// <param name="ms">The memory stream to write the bytes into.</param>
        /// <param name="vector"></param>
        public static void WritePositionLittleEndian(this MemoryStream ms, Vector3 vector)
        {
            ms.WriteFloatLittleEndian(vector.X);
            ms.WriteFloatLittleEndian(vector.Y);
            ms.WriteFloatLittleEndian(vector.Z);
        }

        /// <summary>
        /// Writes a color to the memory stream is BGR order.
        /// </summary>
        /// <param name="ms">The memory stream to write to.</param>
        /// <param name="color">The color to write to the memory stream.</param>
        public static void WriteColor(this MemoryStream ms, Color color)
        {
            ms.WriteByte(color.B);
            ms.WriteByte(color.G);
            ms.WriteByte(color.R);
        }

        /// <summary>
        /// Writes a color to the memory stream is BGRA order.
        /// </summary>
        /// <param name="ms">The memory stream to write to.</param>
        /// <param name="color">The color to write to the memory stream.</param>
        public static void WriteColorAlpha(this MemoryStream ms, Color color)
        {
            ms.WriteByte(color.B);
            ms.WriteByte(color.G);
            ms.WriteByte(color.R);
            ms.WriteByte(color.A);
        }
    }
}
