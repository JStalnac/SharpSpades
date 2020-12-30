using System;

namespace SharpSpades.Api.Configuration.Attributes
{
    /// <summary>
    /// Specifies that this class can be used as a TOML table.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class TableAttribute : Attribute
    {

    }
}
