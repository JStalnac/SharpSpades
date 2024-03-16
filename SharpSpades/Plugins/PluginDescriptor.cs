using Microsoft.Extensions.DependencyInjection;
using SharpSpades.Api.Plugins;
using SharpSpades.Api.Plugins.Attributes;
using System.Collections.Immutable;
using System.Reflection;

#nullable enable

namespace SharpSpades.Plugins;

internal class PluginDescriptor : IPluginDescriptor
{
    public bool Enabled { get; set; }

    public string Id { get; }

    public string Name { get; }

    public string? Description { get; }

    public string? Url { get; }

    public ImmutableArray<string> Authors { get; }

    public Type Type { get; }

    // Should not be modified by plugins
    public IPlugin? Instance { get; set; } = null;
    public IServiceScope? ServiceScope { get; set; } = null;

    public PluginDescriptor(Type type)
    {
        Type = type;

        var info = type.GetCustomAttribute<PluginAttribute>();

        if (info is not null)
        {
            Id = info.Id;
            // Remember to assign later
            Name = info.Name!;

            Description = info.Description;
            Url = info.Url;
        }
        
        Id ??= type.Name;

        // Remove Plugin suffix if any
        Name ??= (type.Name.EndsWith("Plugin")
            ? type.Name.Substring(0, type.Name.Length - 6)
            : type.Name);

        var a = type.GetCustomAttribute<AuthorsAttribute>();
        Authors = a?.Authors.ToImmutableArray()
            ?? ImmutableArray<string>.Empty;
    }
}