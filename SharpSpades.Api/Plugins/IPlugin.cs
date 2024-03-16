namespace SharpSpades.Api.Plugins;

public interface IPlugin
{
    Task EnableAsync();
    Task DisableAsync();
}
