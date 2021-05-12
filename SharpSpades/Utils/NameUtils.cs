#nullable enable

namespace SharpSpades.Utils
{
    public class NameUtils
    {
        /// <summary>
        /// Validates the name. Doesn't check for null.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>True if the name is valid</returns>
        public static bool IsValidName(string? name)
        {
            if (name?.Contains('\x00') ?? false)
                return false;
            if (name?.Length > 16)
                return false;
            
            // TODO: Other restrictions?
            return true;
        }
    }
}