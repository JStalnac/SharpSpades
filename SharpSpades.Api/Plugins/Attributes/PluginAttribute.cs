namespace SharpSpades.Api.Plugins.Attributes;

[AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PluginAttribute : Attribute
{
    public string Id { get; }
    public string? Name { get; }
    public string? Version { get; }
    public string? Description { get; }
    public string? Url { get; }

    public PluginAttribute(string id)
    {
        Id = id;
    }
}