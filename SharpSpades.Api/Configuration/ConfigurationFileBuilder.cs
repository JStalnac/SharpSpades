using System;

#nullable enable

namespace SharpSpades.Api.Configuration
{
    public sealed class ConfigurationFileBuilder
    {
        private TableBuilder? builder = new(null!);

        /// <summary>
        /// Sets the top comment of the file.
        /// See <see cref="TableBuilder.Comment(string)"/>
        /// </summary>
        /// <param name="comment">The comment.</param>
        /// <returns>This object with the new comment assigned.</returns>
        public ConfigurationFileBuilder Comment(string comment)
        {
            builder?.Comment(comment);
            return this;
        }

        /// <summary>
        /// Adds a new table to the configuration.
        /// See <see cref="TableBuilder.Table(string, Action{TableBuilder})"/>
        /// </summary>
        /// <param name="name">Name of the table.</param>
        /// <param name="table">Method to configure the table.</param>
        /// <remarks>Older tables are overwriten.</remarks>
        /// <returns>This object with the new table added.</returns>
        public ConfigurationFileBuilder Table(string name, Action<TableBuilder> table)
        {
            builder?.Table(name, table);
            return this;
        }

        /// <summary>
        /// Adds a new field to the configuration.
        /// See <see cref="TableBuilder.Field(string, Action{FieldBuilder})"/>
        /// </summary>
        /// <param name="name">Name of the field.</param>
        /// <param name="field">Method to configure the field.</param>
        /// <remarks>Older fields are overwriten.</remarks>
        /// <returns>This object with the new field added.</returns>
        public ConfigurationFileBuilder Field(string name, Action<FieldBuilder> field)
        {
            builder?.Field(name, field);
            return this;
        }

        /// <summary>
        /// Builds the <see cref="ConfigurationFileBuilder"/>.
        /// </summary>
        /// <returns>A <see cref="ConfigurationFile"/> representing the result of this builder.</returns>
        public ConfigurationFile Build()
        {
            if (builder is null)
                throw new InvalidOperationException($"This {nameof(ConfigurationFileBuilder)} is already built.");
            var file = new ConfigurationFile(builder.Build()!);
            // Won't be needing this anymore
            builder = null!;
            return file;
        }
    }
}