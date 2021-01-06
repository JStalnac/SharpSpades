using Nett.Coma;
using System;
using System.Linq.Expressions;

#nullable enable

namespace SharpSpades.Api.Utils
{
    public static class Extensions
    {
        /// <summary>
        /// Tries to get the value from <paramref name="config"/>. If the operations fails the error will be returned in<paramref name="error"/>.
        /// </summary>
        /// <typeparam name="T">The type of the configuration.</typeparam>
        /// <typeparam name="TOut">Type of the return value.</typeparam>
        /// <param name="config"></param>
        /// <param name="selector"></param>
        /// <param name="value">The return value of the get operation.</param>
        /// <param name="defaultValue">The default value passed to the get operation. Returned in <paramref name="value"/> if the operation fails.</param>
        /// <param name="error">The error that the config threw if the operation failed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="config"/> is <c>null</c> or <paramref name="selector"/> is <c>null</c>.</exception>
        /// <returns>True if the operations succeeds else False.</returns>
        public static bool TryGet<T, TOut>(this Config<T> config, Expression<Func<T, TOut>> selector, out TOut? value, out Exception? error, TOut defaultValue) where T : class
        {
            Throw.IfNull(config, nameof(config));
            Throw.IfNull(selector, nameof(selector));
            try
            {
                value = config.Get(selector, defaultValue);
                error = null;
            }
            catch (Exception ex)
            {
                error = ex;
                value = defaultValue;
                return false;
            }
            return true;
        }
    }
}
