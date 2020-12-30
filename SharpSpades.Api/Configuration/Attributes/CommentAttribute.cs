using System;

namespace SharpSpades.Api.Configuration.Attributes
{
    /// <summary>
    /// Specifies a comment to be added to a TOML node
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class CommentAttribute : Attribute
    {
        public string Comment { get; }

        public CommentAttribute(string comment)
        {
            Comment = comment;
        }
    }
}
