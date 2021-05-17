using System;
using System.Buffers.Binary;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpSpades.Utils
{
    public static class PacketExtensions
    {
        #region Read methods
        /// <summary>
        /// See <see cref="BinaryPrimitives.ReadUInt32LittleEndian(ReadOnlySpan{byte})"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32LittleEndian(this ReadOnlySpan<byte> buffer, int startIndex)
             => ReadUInt32LittleEndian(buffer[startIndex..]);

        /// <summary>
        /// See <see cref="BinaryPrimitives.ReadUInt32LittleEndian(ReadOnlySpan{byte})"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32LittleEndian(this ReadOnlySpan<byte> buffer)
            => BinaryPrimitives.ReadUInt32LittleEndian(buffer);

        /// <summary>
        /// See <see cref="BinaryPrimitives.ReadSingleLittleEndian(ReadOnlySpan{byte})"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloatLittleEndian(this ReadOnlySpan<byte> buffer, int startIndex)
            => ReadFloatLittleEndian(buffer[startIndex..]);

        /// <summary>
        /// See <see cref="BinaryPrimitives.ReadSingleLittleEndian(ReadOnlySpan{byte})"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloatLittleEndian(this ReadOnlySpan<byte> buffer)
            => BinaryPrimitives.ReadSingleLittleEndian(buffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ReadPosition(this ReadOnlySpan<byte> buffer, int startIndex)
            => ReadPosition(buffer[startIndex..]);

        public static Vector3 ReadPosition(this ReadOnlySpan<byte> buffer)
            => new(ReadFloatLittleEndian(buffer), ReadFloatLittleEndian(buffer, 1), ReadFloatLittleEndian(buffer, 2));

        /// <summary>
        /// See <see cref="ReadColor(ReadOnlySpan{byte})"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color ReadColor(this ReadOnlySpan<byte> buffer, int startIndex)
            => ReadColor(buffer[startIndex..]);

        /// <summary>
        /// Reads a color from the buffer in BGR byte order.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Color ReadColor(this ReadOnlySpan<byte> buffer)
            => Color.FromArgb(buffer[2], buffer[1], buffer[0]);
        #endregion

        #region Write methods
        /// <summary>
        /// See <see cref="BinaryPrimitives.WriteUInt32LittleEndian(Span{byte}, uint)"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="i"></param>
        /// <param name="startIndex"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32LittleEndian(this Span<byte> buffer, uint i, int startIndex)
            => WriteUInt32LittleEndian(buffer[startIndex..], i);

        /// <summary>
        /// See <see cref="BinaryPrimitives.WriteUInt32LittleEndian(Span{byte}, uint)"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="i"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32LittleEndian(this Span<byte> buffer, uint i)
            => BinaryPrimitives.WriteUInt32LittleEndian(buffer, i);

        /// <summary>
        /// See <see cref="BinaryPrimitives.WriteSingleLittleEndian(Span{byte}, float)"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="f"></param>
        /// <param name="startIndex"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloatLittleEndian(this Span<byte> buffer, float f, int startIndex)
            => WriteFloatLittleEndian(buffer[startIndex..], f);

        /// <summary>
        /// See <see cref="BinaryPrimitives.WriteSingleLittleEndian(Span{byte}, float)"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="f"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloatLittleEndian(this Span<byte> buffer, float f)
            => BinaryPrimitives.WriteSingleLittleEndian(buffer, f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WritePosition(this Span<byte> buffer, Vector3 vector, int startIndex)
            => WritePosition(buffer[startIndex..], vector);

        public static void WritePosition(this Span<byte> buffer, Vector3 vector)
        {
            buffer.WriteFloatLittleEndian(vector.X);
            buffer.WriteFloatLittleEndian(vector.Y, 4);
            buffer.WriteFloatLittleEndian(vector.Z, 8);
        }

        /// <summary>
        /// See <see cref="WriteColor(Span{byte}, Color)"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startIndex"></param>
        /// <param name="color"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteColor(this Span<byte> buffer, Color color, int startIndex)
            => WriteColor(buffer[startIndex..], color);

        /// <summary>
        /// Writes a color to the buffer in BGR byte order.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="color"></param>
        public static void WriteColor(this Span<byte> buffer, Color color)
        {
            buffer[0] = color.B;
            buffer[1] = color.G;
            buffer[2] = color.R;
        }
        #endregion
    }
}
