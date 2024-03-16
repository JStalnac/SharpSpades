namespace SharpSpades.Api.Plugins.Attributes;

[AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DependsAttribute : Attribute
{
    public string[] Tags { get; }

    public DependsAttribute(params string[] tags)
    {
        Tags = tags
            .Where(a => a is not null && a != "")
            .ToArray();
    }
}