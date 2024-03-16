namespace SharpSpades.Api.Plugins.Attributes;

[AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AuthorsAttribute : Attribute
{
    public string[] Authors { get; }

    public AuthorsAttribute(params string[] authors)
    {
        Authors = authors
            .Where(a => a is not null && a != "")
            .ToArray();
    }
}