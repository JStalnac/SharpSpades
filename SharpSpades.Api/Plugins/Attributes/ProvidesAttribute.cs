namespace SharpSpades.Api.Plugins.Attributes;

[AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ProvidesAttribute : Attribute
{
    public string[] Tags { get; }

    public ProvidesAttribute(params string[] tags)
    {
        Tags = tags
            .Where(a => a is not null && a != "")
            .ToArray();
    }
}