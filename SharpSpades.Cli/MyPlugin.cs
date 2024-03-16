using Microsoft.Extensions.Logging;
using SharpSpades.Api.Plugins;

namespace SharpSpades.Cli;

public class MyPlugin : IPlugin
{
    private readonly ILogger<MyPlugin> logger;

    public MyPlugin(ILogger<MyPlugin> logger)
    {
        this.logger = logger;
    }

    public Task EnableAsync()
    {
        logger.LogInformation("Hello, World!");
        return Task.CompletedTask;
    }

    public Task DisableAsync()
    {
        logger.LogInformation("Goodbye, cruel world!");
        return Task.CompletedTask;
    }
}