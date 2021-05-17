using System;
using System.Text;

namespace SharpSpades.Utils
{
    public static class StringUtils
    {
        // IBM437
        private static Encoding _cp437;

        private static bool _hasRegisteredEncoding;

        private static void RegisterEncoding()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _cp437 = Encoding.GetEncoding(437);

            _hasRegisteredEncoding = true;
        }

        /// <summary>
        /// Decodes the string as a CP437 string and returns the bytes.
        /// </summary>
        /// <param name="s"></param>
        public static byte[] ToCP437String(this string s)
        {
            if (!_hasRegisteredEncoding)
                RegisterEncoding();

            Throw.IfNull(s, nameof(s));

            return _cp437.GetBytes(s);
        }

        /// <summary>
        /// Reads a CP437 string from the buffer and decodes it into a string.
        /// </summary>
        /// <param name="buffer">The buffer to read the string from.</param>
        public static string ReadCP437String(this ReadOnlySpan<byte> buffer)
        {
            if (!_hasRegisteredEncoding)
                RegisterEncoding();

            return _cp437.GetString(buffer);
        }

        /// <summary>
        /// Check if a string is null or empty or contains a white space.
        /// </summary>
        /// <param name="s">The string.</param>
        public static bool IsNullOrEmptyOrWhiteSpace(this string s) => string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s);
    }
}
