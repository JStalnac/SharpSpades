using System;

#nullable enable

namespace SharpSpades.Api.Utils
{
    public static class Throw
    {
        public static void IfNull<T>(T? obj) where T : class
        {
            if (obj is null)
                throw new ArgumentNullException();
        }

        public static void IfNull<T>(T? obj, string? paramName) where T : class
        {
            if (obj is null)
                throw new ArgumentNullException(paramName);
        }

        public static void IfNull<T>(T? obj, string? paramName, string? message) where T : class
        {
            if (obj is null)
                throw new ArgumentNullException(paramName, message);
        }
    }
}
