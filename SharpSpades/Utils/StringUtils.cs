using System;
using System.Text;

namespace SharpSpades.Utils
{
    public static class StringUtils
    {
        private static Encoding cp437Encoding;

        static StringUtils()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            cp437Encoding = Encoding.GetEncoding(437);
        }

        /// <summary>
        /// Decodes the string as a CP437 string and returns the bytes.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static byte[] ToCP437String(string s)
        {
            Throw.IfNull(s, nameof(s));
            return cp437Encoding.GetBytes(s);
        }

        /// <summary>
        /// Reads a CP437 string from the buffer and decodes it into a string.
        /// </summary>
        /// <param name="buffer">The buffer to read the string from.</param>
        /// <returns></returns>
        public static string ReadCP437String(ReadOnlySpan<byte> buffer)
            => cp437Encoding.GetString(buffer);
    }
}
