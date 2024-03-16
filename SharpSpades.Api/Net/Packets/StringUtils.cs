using System.Text;

namespace SharpSpades.Api.Net.Packets
{
    internal static class StringUtils
    {
        // IBM437
        private static Encoding _cp437;

        static StringUtils()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _cp437 = Encoding.GetEncoding(437);
        }

        /// <summary>
        /// Decodes the string as a CP437 string and returns the bytes.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static byte[] ToCP437String(this string s)
        {
            ArgumentNullException.ThrowIfNull(s);

            return _cp437.GetBytes(s);
        }

        /// <summary>
        /// Reads a CP437 string from the buffer and decodes it into a string.
        /// </summary>
        /// <param name="buffer">The buffer to read the string from.</param>
        /// <returns></returns>
        public static string ReadCP437String(this ReadOnlySpan<byte> buffer)
        {
            return _cp437.GetString(buffer);
        }

        /// <summary>
        /// Check if a string is null or empty or contains a white space.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns></returns>
        public static bool IsNullOrEmptyOrWhiteSpace(this string? s) => String.IsNullOrEmpty(s) || String.IsNullOrWhiteSpace(s);

        /// <summary>
        /// Generates a standard message that can be used in the <see cref="ArgumentNullException"/> or <see cref="NullReferenceException"/>.
        /// </summary>
        /// <param name="paramName">Name of the <see langword="null"/> variable.</param>
        /// <returns></returns>
        public static string GenerateNullExceptionMessage(string? paramName = null)
            => paramName.IsNullOrEmptyOrWhiteSpace() ? "This cannot be null!" : $"The {nameof(paramName)} cannot be null!";
    }
}