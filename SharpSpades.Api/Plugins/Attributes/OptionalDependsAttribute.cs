namespace SharpSpades.Api.Plugins.Attributes;

[AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class OptionalDependsAttribute : Attribute
{
    public string[] Tags { get; }

    public OptionalDependsAttribute(params string[] tags)
    {
        Tags = tags
            .Where(a => a is not null && a != "")
            .ToArray();
    }
}