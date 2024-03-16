using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpSpades.Api.Plugins;
using SharpSpades.Utils;
using System.Collections.Immutable;
using System.Reflection;

#nullable enable

namespace SharpSpades.Plugins;

public class PluginManager : IPluginManager
{
    private ImmutableArray<PluginDescriptor> Plugins { get; set; } = ImmutableArray<PluginDescriptor>.Empty;

    private bool allRegistered;

    private IServiceProvider? serviceProvider;
    private ILogger<PluginManager>? logger;

    public void RegisterPlugin<T>() where T : IPlugin
        => RegisterPlugin(typeof(T));

    public void RegisterPlugin(Assembly assembly)
    {
        Throw.IfNull(assembly);

        if (allRegistered)
            throw new InvalidOperationException("Cannot register plugins after plugin registration is complete");

        Type plugin;
        try
        {
            plugin = assembly.GetExportedTypes()
                .SingleOrDefault(t => t.IsAssignableTo(typeof(IPlugin)))
                ?? throw new ArgumentException("Could not find public plugin type from assembly");
        }
        catch (InvalidOperationException)
        {
            throw new ArgumentException($"Assembly contains more than one plugin type");
        }
        RegisterPlugin(plugin);
    }

    public void RegisterPlugin(Type type)
    {
        Throw.IfNull(type);

        if (type.IsInterface || type.IsAbstract)
            throw new ArgumentException("Type must not be an abstract type");

        if (type.IsGenericType)
            throw new ArgumentException("Type must not be generic");

        if (!type.IsAssignableTo(typeof(IPlugin)))
            throw new ArgumentException($"Plugin must inherit from {typeof(IPlugin).Name}");

        if (allRegistered)
            throw new InvalidOperationException("Cannot register plugins after plugin registration is complete");

        Plugins = Plugins.Add(new PluginDescriptor(type));
    }

    public void BuildPluginTree()
    {
        // TODO: Check for missing dependencies and cycles etc.
        allRegistered = true;
    }

    public void RegisterServices(IServiceCollection serviceCollection)
    {
        if (!allRegistered)
            throw new InvalidOperationException("All plugins must be registered before plugin service registration (Call BuildPluginTree() first)");

        var types = Plugins.Select(p => p.Type)
            .ToArray();

        foreach (var type in types)
        {
            var method = type.GetMethod("RegisterServices", BindingFlags.Public | BindingFlags.Static,
                new[] { typeof(IServiceCollection) });

            if (method is null)
                continue;

            method.Invoke(null, new object[] { serviceCollection });
        }
    }

    internal void RegisterServiceProvider(IServiceProvider serviceProvider)
    {
        if (!allRegistered)
            throw new InvalidOperationException("All plugins must be registered before (Call BuildPluginTree() first)");

        this.serviceProvider = serviceProvider;
        logger = serviceProvider.GetRequiredService<ILogger<PluginManager>>();
    }

    public IEnumerable<IPluginDescriptor> GetPlugins()
    {
        if (serviceProvider is null)
            throw new InvalidOperationException("The server has not started");

        return Plugins;
    }

    public IPluginDescriptor? GetPlugin<T>() where T : IPlugin
        => GetPlugin(typeof(T));

    public IPluginDescriptor? GetPlugin(Type type)
    {
        Throw.IfNull(type);

        if (!type.IsAssignableTo(typeof(IPlugin)))
            throw new ArgumentException($"Type must inherit {typeof(IPlugin).Name}");

        if (serviceProvider is null)
            throw new InvalidOperationException("The server has not started");

        return Plugins.SingleOrDefault(p => p.Type == type);
    }

    public async Task<bool> EnableAsync(IPluginDescriptor plugin)
    {
        if (!allRegistered || serviceProvider is null || logger is null)
            throw new InvalidOperationException("The server has not started");

        if (plugin is not PluginDescriptor p)
            throw new ArgumentException("Invalid plugin descriptor");

        if (p.Enabled || p.Instance is not null)
            return true;

        // TODO: Enable dependencies

        // TODO: If a plugin is started from two threads at the same time,
        // one of the threads will return before the plugin has actually been
        // enabled. Issue only when plugins can have dependencies or a plugin
        // provides features for managing other plugins.
        // Also race conditions are possible.

        logger.LogInformation("Enabling plugin {Name}", p.Id);

        try
        {
            var scope = serviceProvider.CreateScope();
            p.ServiceScope = scope;

            p.Instance = (IPlugin)ActivatorUtilities
                .CreateInstance(scope.ServiceProvider, p.Type);

            await p.Instance.EnableAsync();

            p.Enabled = true;
            logger.LogInformation("Enabled plugin {Name}", p.Id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enable plugin {Name}", p.Id);
            return false;
        }
    }

    public async Task DisableAsync(IPluginDescriptor plugin)
    {
        if (!allRegistered || logger is null)
            throw new InvalidOperationException("The server has not started");

        if (plugin is not PluginDescriptor p)
            throw new ArgumentException("Invalid plugin descriptor");

        if (!p.Enabled && p.Instance is not null)
            throw new InvalidOperationException("The plugin is starting");

        if (p.Instance is null)
            return;

        logger.LogInformation("Disabling plugin {Name}", p.Id);

        try
        {
            await p.Instance.DisableAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while disabling plugin {Name}", p.Id);
        }
        finally
        {
            p.Instance = null;
            p.ServiceScope!.Dispose();
            p.ServiceScope = null;
            p.Enabled = false;
            logger.LogInformation("Disabled plugin {Name}", p.Id);
        }
    }
}