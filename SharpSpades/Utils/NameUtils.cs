#nullable enable

namespace SharpSpades.Utils
{
    public static class NameUtils
    {
        /// <summary>
        /// Validates the name. Doesn't check for null.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>True if the name is valid</returns>
        public static bool IsValidName(string? name)
        {
            if (String.IsNullOrEmpty(name))
                return false;
            
            if (name.Contains('\x00'))
                return false;

            if (name.Length > 15)
                return false;

            // TODO: Other restrictions?
            return true;
        }
    }
}