using System.Collections.Immutable;

namespace SharpSpades.Api.Plugins;

public interface IPluginDescriptor
{
    bool Enabled { get; }
    string Id { get; }
    string Name { get; }
    string? Description { get; }
    string? Url { get; }
    ImmutableArray<string> Authors { get; }
    Type Type { get; }
}