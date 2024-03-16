using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharpSpades.Api.Events;
using SharpSpades.Events;

namespace SharpSpades;

public static class HostBuilderExtensions
{
    public static IHostBuilder AddSharpSpades(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddHostedService<Server>();
            services.AddSingleton<IEventManager, EventManager>();
        });
        return hostBuilder;
    }
}