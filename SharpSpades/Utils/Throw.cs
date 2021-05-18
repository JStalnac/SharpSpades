using System;

#nullable enable

namespace SharpSpades.Utils
{
    internal static class Throw
    {
        public static void IfNull<T>(T? obj, string? paramName = null, string? message = null)
        {
            if (obj is null)
                throw new ArgumentNullException(paramName, message);
        }

        public static void IfNotNull<T>(T? obj, string? message = null)
        {
            if (obj is not null)
                throw new InvalidOperationException(message);
        }
    }
}
