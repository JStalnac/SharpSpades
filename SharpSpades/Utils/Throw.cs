using System;

#nullable enable

namespace SharpSpades.Utils
{
    /// <summary>
    /// Static class that throws exceptions.
    /// </summary>
    internal static class Throw
    {
        /// <summary>
        /// Checks if an object is null and throws the <see cref="ArgumentNullException"/> exception.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object to check if it is null.</param>
        /// <param name="paramName">The name of this object.</param>
        public static void IfNull<T>(T? obj, string? paramName = null)
            => IfNull(obj, paramName, null);

        /// <summary>
        /// Checks if an object is null and throws the <see cref="ArgumentNullException"/> exception.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object to check if it is null.</param>
        /// <param name="paramName">The name of this object.</param>
        /// <param name="message">A custom message to be displayed when the exception is thrown.</param>
        public static void IfNull<T>(T? obj, string? paramName = null, string? message = null)
        {
            if (obj is null)
                throw new ArgumentNullException(paramName, message);
        }

        /// <summary>
        /// Checks if an object is not null and throws the <see cref="InvalidOperationException"/> exception.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object to check if it is not null.</param>
        /// <param name="message">A custom message to be displayed when the exception is thrown.</param>
        public static void IfNotNull<T>(T? obj, string? message = null)
        {
            if (obj is not null)
                throw new InvalidOperationException(message);
        }
    }
}
