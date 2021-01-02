using SharpSpades.Api.Utils;
using System;
using Tommy;

#nullable enable

namespace SharpSpades.Api.Configuration
{
    /// <summary>
    /// A builder to configure a field inside a <see cref="TableBuilder"/>.
    /// This class cannot be inherited.
    /// </summary>
    public sealed class FieldBuilder
    {
        private string name { get; }
        internal TomlNode? value;
        private Func<TomlNode, bool>? validator;
        private bool required;

        internal FieldBuilder(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Sets the initial value of the field.
        /// </summary>
        /// <param name="value">The initial value</param>
        /// <returns>This object with the new initial value assigned.</returns>
        public FieldBuilder InitialValue(TomlNode value)
        {
            Throw.IfNull(value, nameof(value));

            if (value is TomlTable)
                throw new InvalidOperationException($"{nameof(TomlTable)} is invalid in this context");

            if (value is TomlArray array)
                ValidateArray(array);

            this.value = value;
            return this;
        }
        
        /// <summary>
        /// Adds a validator function to the field to validate new values.
        /// The validator returns True if the value is valid, else False.
        /// If a validator fails the whole configuration will be discarded when updating.
        /// </summary>
        /// <param name="validate"></param>
        /// <returns>This object with the new validator assinged.</returns>
        public FieldBuilder Validate(Func<TomlNode, bool> validate)
        {
            Throw.IfNull(validate);
            validator = validate;
            return this;
        }

        /// <summary>
        /// Marks the field as required.
        /// If a field is missing the whole configuration is discarded when updating.
        /// </summary>
        /// <returns>This object with the new property assigned.</returns>
        public FieldBuilder Required()
        {
            required = true;
            return this;
        }

        /// <summary>
        /// Sets the comment for the field.
        /// </summary>
        /// <param name="comment">The new comment.</param>
        /// <returns>This object with the new comment assigned.</returns>
        public FieldBuilder Comment(string? comment)
        {
            if (value is null)
                throw new InvalidOperationException($"The field's initial value must be set before setting the comment");
            value.Comment = comment;
            return this;
        }

        internal Field Build()
            => new()
            {
                Name = name,
                Comment = value!.Comment,
                InitialValue = value,
                Required = required,
                Validator = validator,
                Type = value.GetType(),
            };

        private static void ValidateArray(TomlArray array)
        {
            Type? arrayType = null;
            for (int i = 0; i < array.ChildrenCount; i++)
            {
                if (i == 0)
                {
                    arrayType = array[0].GetType();
                    if (arrayType == typeof(TomlTable))
                        throw new InvalidOperationException($"{nameof(TomlTable)} is invalid in this context");
                    if (arrayType == typeof(TomlArray))
                        ValidateArray((TomlArray)array[0]);
                    continue;
                }

                if (array[i].GetType() != arrayType)
                    throw new InvalidOperationException($"Element type {array[i].GetType()} is not valid for an array of type {arrayType}");
                if (arrayType == typeof(TomlArray))
                    ValidateArray((TomlArray)array[i]);
            }
        }
    }
}
