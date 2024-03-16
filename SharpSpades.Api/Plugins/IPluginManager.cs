namespace SharpSpades.Api.Plugins;

/// <summary>
/// A service that manages the plugins loaded into the server.
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// Enables the provided plugin.
    /// </summary>
    /// <param name="plugin">The plugin to enable.</param>
    /// <returns>True if the plugin is successfully enabled, otherwise False.</returns>
    Task<bool> EnableAsync(IPluginDescriptor plugin);

    /// <summary>
    /// Disables the plugin.
    /// </summary>
    /// <param name="plugin">The plugin to disable.</param>
    /// <returns></returns>
    Task DisableAsync(IPluginDescriptor plugin);

    /// <summary>
    /// Gets a plugin of the provided type from the <see cref="IPluginManager" />
    /// </summary>
    /// <param name="type">The type of the plugin to search for.</param>
    /// <returns>The found plugin or null.</returns>
    IPluginDescriptor? GetPlugin<T>() where T : IPlugin;

    /// <summary>
    /// Gets a plugin of the provided type from the <see cref="IPluginManager" />
    /// </summary>
    /// <param name="type">The type of the plugin to search for.</param>
    /// <returns>The found plugin or null.</returns>
    IPluginDescriptor? GetPlugin(Type type);

    /// <summary>
    /// Gets all the plugins loaded into the <see cref="IPluginManager" />.
    /// </summary>
    /// <returns>All the loaded plugins.</returns>
    IEnumerable<IPluginDescriptor> GetPlugins();
}