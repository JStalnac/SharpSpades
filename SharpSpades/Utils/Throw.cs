using System;

#nullable enable

namespace SharpSpades.Utils
{
    internal static class Throw
    {
        public static void If<T>(T? obj, Predicate<T?> predicate, Exception exception) //where T : class
        {
            if (predicate.Invoke(obj))
                throw exception;
        }

        public static void IfNull<T>(T? obj, Exception exception) //where T : class
            => If(obj, x => x is null, exception);

        public static void IfNotNull<T>(T? obj, Exception exception)
            => If(obj, x => x is not null, exception);
    }
}
