using System;
using System.Text;

namespace SharpSpades.Utils
{
    public static class StringUtils
    {
        // IBM437
        private static Encoding _ibm437;

        private static bool _hasRegisteredEncoding;

        private static void RegisterEncoding()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _ibm437 = Encoding.GetEncoding(437);

            _hasRegisteredEncoding = true;
        }

        /// <summary>
        /// Decodes the string as a IBM437 string and returns the bytes.
        /// </summary>
        /// <param name="s"></param>
        public static byte[] ToCP437String(this string s)
        {
            if (!_hasRegisteredEncoding)
                RegisterEncoding();

            Throw.IfNull(s, nameof(s));

            return _ibm437.GetBytes(s);
        }

        /// <summary>
        /// Reads a IBM437 string from the buffer and decodes it into a string.
        /// </summary>
        /// <param name="buffer">The buffer to read the string from.</param>
        public static string ReadCP437String(this ReadOnlySpan<byte> buffer)
        {
            if (!_hasRegisteredEncoding)
                RegisterEncoding();

            return _ibm437.GetString(buffer);
        }
    }
}
