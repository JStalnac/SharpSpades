using SharpSpades.Api.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace SharpSpades.Api.Configuration
{
    /// <summary>
    /// A builder to configure a table inside a <see cref="ConfigurationFileBuilder"/>.
    /// This class cannot be inherited.
    /// </summary>
    public sealed class TableBuilder
    {
        public string name;
        private string? comment;
        private bool required;
        internal Dictionary<string, TableBuilder> Tables { get; } = new();
        internal Dictionary<string, FieldBuilder> Fields { get; } = new();

        internal TableBuilder(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Sets the TOML comment for the table.
        /// </summary>
        /// <param name="comment">The new comment for the table.</param>
        /// <returns>This object with the new comment assigned.</returns>
        public TableBuilder Comment(string comment)
        {
            this.comment = comment;
            return this;
        }

        /// <summary>
        /// Marks the table as required.
        /// If a required table is missing the whole configuration will be discarded when updating.
        /// </summary>
        /// <returns>This object with the new property assigned.</returns>
        public TableBuilder Required()
        {
            required = true;
            return this;
        }

        /// <summary>
        /// Adds a new table under this table.
        /// </summary>
        /// <param name="name">Name of the table.</param>
        /// <param name="table">Method to configure the table.</param>
        /// <remarks>Older tables a overwriten.</remarks>
        /// <returns>This object with the new table added.</returns>
        public TableBuilder Table(string name, Action<TableBuilder> table)
        {
            Throw.IfNull(name, nameof(name));
            Throw.IfNull(table, nameof(table));
            ThrowIfNameNotValid(name);
            // Without this the TOML will be written with errors if the name has spaces.
            string n = name.Replace(' ', '-');
            var tb = new TableBuilder(n);
            table(tb);
            Tables[n] = tb;
            return this;
        }
        
        /// <summary>
        /// Adds a new field to this table.
        /// </summary>
        /// <param name="name">Name of the field.</param>
        /// <param name="field">Method to configure the field.</param>
        /// <remarks>Older fields are overwriten.</remarks>
        /// <exception cref="ArgumentException">The field builder method doesn't set the initial value of the field.</exception>
        /// <returns>This object with the new field added.</returns>
        public TableBuilder Field(string name, Action<FieldBuilder> field)
        {
            Throw.IfNull(name, nameof(name));
            Throw.IfNull(field, nameof(field));
            ThrowIfNameNotValid(name);
            var fb = new FieldBuilder(name);
            field(fb);
            if (fb.value is null)
                throw new ArgumentException($"The initial value of the field must be set in the builder. (Field: '{name}')");
            Fields[name] = fb;
            return this;
        }

        internal Table Build()
        {
            var tables = Tables.Select(t => t.Value.Build()).ToArray();
            var fields = Fields.Select(f => f.Value.Build()).ToArray();
            return new()
            {
                Comment = comment,
                Name = name,
                Required = required,
                Tables = tables,
                Fields = fields
            };
        }

        private void ThrowIfNameNotValid(string name)
        {
            if (name.Contains('\x001f') || name.Contains('\x007f'))
                throw new ArgumentException("Name may not contain \\x001f or \\x007f characters!");
        }
    }
}
