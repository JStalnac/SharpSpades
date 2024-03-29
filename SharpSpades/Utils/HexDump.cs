﻿using System.Text;

namespace SharpSpades.Utils
{
    public static class HexDump
    {
        public static string Create(ReadOnlySpan<byte> buffer, int bytesPerLine = 16)
        {
            var result = new StringBuilder();

            for (int offset = 0; offset < buffer.Length; offset += bytesPerLine)
            {
                result.Append($"{offset:X4}\t");

                for (int i = 0; i < bytesPerLine; i++)
                {
                    if (offset + i >= buffer.Length)
                        break;

                    result.Append($" {buffer[offset + i]:x2}");
                }

                result.AppendLine();
            }

            return result.ToString();
        }
    }
}