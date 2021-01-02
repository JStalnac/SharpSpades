using System;
using Tommy;

#nullable enable

namespace SharpSpades.Api.Configuration
{
    public class ConfigurationFile
    {
        internal TomlTable? toml;
        private readonly Table data;
        /// <summary>
        /// Used for locking the object for data access.
        /// </summary>
        internal readonly object locker = new();

        internal ConfigurationFile(Table data)
        {
            this.data = data;
        }

        public TomlTable GetConfiguration()
        {
            if (toml is null)
                throw new InvalidOperationException($"The {nameof(ConfigurationFile)} must be added to a {nameof(ConfigurationManager)} before accessing its configuration");
            return toml;
        }
    }
}
