using System.Runtime.CompilerServices;

#nullable enable

namespace SharpSpades.Utils
{
    /// <summary>
    /// Provides static helpers methods for throwing exceptions.
    /// </summary>
    internal static class Throw
    {
        /// <summary>
        /// Checks if an object is null and throws the <see cref="ArgumentNullException"/> exception.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object to check if it is null.</param>
        /// <param name="paramName">The name of this object.</param>
        public static void IfNull<T>(T? obj, [CallerArgumentExpression("obj")] string? paramName = null)
        {
            if (obj is null)
                throw new ArgumentNullException(paramName);
        }
    }
}