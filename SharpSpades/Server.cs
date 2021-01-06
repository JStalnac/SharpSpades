using ENet.Managed;
using Microsoft.Extensions.Logging;
using Nett;
using Nett.Coma;
using Serilog;
using Serilog.Events;
using SharpSpades.Api;
using SharpSpades.Api.Utils;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SharpSpades
{
    public class Server : IServer
    {
        public readonly CancellationTokenSource cts = new();
        public Config<ServerConfiguration> Config { get; }
        public ILoggerFactory LoggerFactory { get; }
        public ILogger<Server> Logger { get; }
        
        public Server(string configurationDirectory)
        {
            Throw.IfNull(configurationDirectory, nameof(configurationDirectory));

            Toml.WriteFile<ServerConfiguration>(new(), "config.toml");
            
            Config = Nett.Coma.Config.CreateAs()
                .MappedToType(() => new ServerConfiguration())
                .StoredAs(store => store.File("config.toml").AccessedBySource("app", out _))
                .Initialize();

            var loggingConfig = Config.Get(x => x.LogLevels, new());
            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(c =>
            {
                var logger = new LoggerConfiguration()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
                if (!String.IsNullOrEmpty(loggingConfig.LogFile))
                {
                    logger.WriteTo.File(path: null,
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
                }

                // Apply default log level
                LogEventLevel level;
                var defaultLevel = GetLevel(loggingConfig.Default);
                if (!defaultLevel.HasValue)
                    level = LogEventLevel.Information;
                else
                    level = defaultLevel.Value;
                logger.MinimumLevel.Is(level);

                // Apply log level overrides
                foreach (string s in loggingConfig.Trace)
                    logger.MinimumLevel.Override(s, LogEventLevel.Verbose);
                foreach (string s in loggingConfig.Debug)
                    logger.MinimumLevel.Override(s, LogEventLevel.Debug);
                foreach (string s in loggingConfig.Information)
                    logger.MinimumLevel.Override(s, LogEventLevel.Information);
                foreach (string s in loggingConfig.Warning)
                    logger.MinimumLevel.Override(s, LogEventLevel.Warning);
                foreach (string s in loggingConfig.Error)
                    logger.MinimumLevel.Override(s, LogEventLevel.Error);
                foreach (string s in loggingConfig.Fatal)
                    logger.MinimumLevel.Override(s, LogEventLevel.Fatal);
                
                c.AddSerilog(logger.CreateLogger(), true);
            });
            
            Logger = LoggerFactory.CreateLogger<Server>();
        }

        public ILogger<T> GetLogger<T>()
            => LoggerFactory.CreateLogger<T>();

        public Microsoft.Extensions.Logging.ILogger GetLogger(string categoryName)
            => LoggerFactory.CreateLogger(categoryName);

        public Task StartAsync()
        {
            ushort port = 32887;

            Logger.LogInformation("Loading");
            if (!ManagedENet.Started)
            {
                Logger.LogInformation("Loading ENet libraries...");
                ManagedENet.Startup();
            }

            var listenEndPoint = new IPEndPoint(IPAddress.Loopback, port);

            Logger.LogDebug("Creating host...");
            ENetHost host;
            try
            {
                host = new ENetHost(listenEndPoint, 1, 1);
            }
            catch (NullReferenceException)
            {
                Logger.LogCritical("Failed to initialize ENet. Is the library initialized?");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Failed to create host");
                return Task.CompletedTask;
            }

            // Important for the clients to be able to connect
            host.CompressWithRangeCoder();

            Logger.LogInformation($"Ready");
            Logger.LogDebug($"Listening on port {port}");

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var ev = host.Service(TimeSpan.FromMilliseconds(10));

                    switch (ev.Type)
                    {
                        case ENetEventType.None:
                            continue;
                        case ENetEventType.Connect:
                            Logger.LogInformation($"Peer connected: {ev.Peer.GetRemoteEndPoint()}");
                            if (ev.Data != 3)
                                // Only v0.75 is supported
                                ev.Peer.Disconnect((uint)DisconnectReason.WrongProtocolVersion);
                            continue;
                        case ENetEventType.Disconnect:
                            Logger.LogInformation($"Peer disconnected: {ev.Peer.GetRemoteEndPoint()}");
                            continue;
                        case ENetEventType.Receive:
                            Logger.LogInformation($"Received data: {ev.Peer.GetRemoteEndPoint()}: {string.Join("", ev.Packet.Data.ToArray())}");
                            ev.Packet.Destroy();
                            continue;
                    }
                }
                
            }
            finally
            {
                Logger.LogDebug("Disposing host");
                host.Dispose();
            }

            // TODO: Handle Ctrl + C
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            cts.Cancel();
            return Task.CompletedTask;
        }

        private static LogEventLevel? GetLevel(string level)
        {
            return level switch
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
