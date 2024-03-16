using ENet.Managed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using SharpSpades.Api.Plugins;
using SharpSpades.Plugins;
using System.Text.Json;

namespace SharpSpades.Cli
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                string configDirectory = Directory.GetCurrentDirectory();

                // Create the config file if it doesn't exist
                string configFile = Path.Combine(configDirectory, "config.json");
                if (!File.Exists(configFile))
                {
                    await File.WriteAllTextAsync(configFile, JsonSerializer.Serialize(new DefaultConfiguration(), new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
                }

                var config = new ConfigurationBuilder()
                    .SetBasePath(configDirectory)
                    .AddJsonFile("config.json")
                    .Build();

                // Register plugins
                var pm = new PluginManager();
                pm.RegisterPlugin<MyPlugin>();

                pm.BuildPluginTree();

                var host = CreateHostBuilder().ConfigureServices(services =>
                {
                    services.AddSingleton<PluginManager>(pm);
                    services.AddSingleton<IPluginManager>(pm);
                    // Register plugin services
                    pm.RegisterServices(services);
                }).Build();

                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    host.Services.GetRequiredService<IHostApplicationLifetime>()
                        .StopApplication();
                };

                await host.RunAsync();
            }
            finally
            {
                ManagedENet.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder()
            => new HostBuilder()
                .AddSharpSpades()
                .ConfigureAppConfiguration(c =>
                {
                    c.SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("config.json");
                })
                .ConfigureLogging((host, c) =>
                {
                    var loggingConfig = host.Configuration.GetSection("LogLevels");

                    string logFile = loggingConfig["LogFile"];
                    var logger = new LoggerConfiguration()
                        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
                    if (!String.IsNullOrEmpty(logFile))
                    {
                        bool rollDaily = loggingConfig.GetValue<bool>("RollDaily", true);
                        logger.WriteTo.File(logFile,
                            rollingInterval: rollDaily ? RollingInterval.Day : RollingInterval.Infinite,
                            shared: true, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
                    }

                    // Apply default log level
                    logger.MinimumLevel.Is(GetLevel(loggingConfig["Default"]) ?? LogEventLevel.Information);

                    // Apply log level overrides
                    foreach (string s in loggingConfig.GetSection("Trace").Get<string[]>() ?? Array.Empty<string>())
                        logger.MinimumLevel.Override(s, LogEventLevel.Verbose);
                    foreach (string s in loggingConfig.GetSection("Debug").Get<string[]>() ?? Array.Empty<string>())
                        logger.MinimumLevel.Override(s, LogEventLevel.Debug);
                    foreach (string s in loggingConfig.GetSection("Information").Get<string[]>() ?? Array.Empty<string>())
                        logger.MinimumLevel.Override(s, LogEventLevel.Information);
                    foreach (string s in loggingConfig.GetSection("Warning").Get<string[]>() ?? Array.Empty<string>())
                        logger.MinimumLevel.Override(s, LogEventLevel.Warning);
                    foreach (string s in loggingConfig.GetSection("Error").Get<string[]>() ?? Array.Empty<string>())
                        logger.MinimumLevel.Override(s, LogEventLevel.Error);
                    foreach (string s in loggingConfig.GetSection("Fatal").Get<string[]>() ?? Array.Empty<string>())
                        logger.MinimumLevel.Override(s, LogEventLevel.Fatal);

                    logger.MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning);

                    c.AddSerilog(logger.CreateLogger(), true);
                });

        private static LogEventLevel? GetLevel(string level)
        {
            return level.ToLower() switch
            {
                "trace" or "verbose" => LogEventLevel.Verbose,
                "debug" => LogEventLevel.Debug,
                "information" or "info" => LogEventLevel.Information,
                "warning" or "warn" => LogEventLevel.Warning,
                "error" => LogEventLevel.Error,
                "fatal" or "critical" or "crit" => LogEventLevel.Fatal,
                _ => null
            };
        }
    }
}