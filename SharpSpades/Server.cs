﻿using ENet.Managed;
using ENet.Managed.Async;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nett;
using Serilog;
using Serilog.Events;
using SharpSpades.Net;
using SharpSpades.Utils;
using SharpSpades.Vxl;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace SharpSpades
{
    public class Server
    {
        public readonly CancellationTokenSource cts = new();
        public IConfigurationRoot Configuration { get; }
        public ILoggerFactory LoggerFactory { get; }
        public ILogger<Server> Logger { get; }
        public string RootDirectory { get; }
        public ConcurrentDictionary<ENetAsyncPeer, Client> Clients { get; } = new();
        public World? World { get; private set; }

        private volatile bool started;

        public const short MaxPlayers = 32;

        public Server(string configurationDirectory)
        {
            Throw.IfNull(configurationDirectory, nameof(configurationDirectory));

            RootDirectory = Path.IsPathRooted(configurationDirectory)
                ? configurationDirectory
                : Path.Combine(Directory.GetCurrentDirectory(), configurationDirectory);

            // Create the config file
            string configFile = Path.Combine(RootDirectory, "config.toml");
            if (!File.Exists(configFile))
                Toml.WriteFile<ServerConfiguration>(new ServerConfiguration(), configFile);

            try
            {
                Configuration = new ConfigurationBuilder()
                    .SetBasePath(RootDirectory)
                    .AddTomlFile("config.toml")
                    .Build();
            }
            catch
            {
                Console.WriteLine($"Failed to load configuration");
                throw;
            }

            LoggingConfiguration loggingConfig;
            try
            {
                loggingConfig = Configuration.GetSection("LogLevels").Get<LoggingConfiguration>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load logging config. Using default logging config. Exception:\n{ex}");
                loggingConfig = new LoggingConfiguration();
            }

            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(c =>
            {
                var logger = new LoggerConfiguration()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
                if (!String.IsNullOrEmpty(loggingConfig.LogFile))
                {
                    logger.WriteTo.File(loggingConfig.LogFile,
                        rollingInterval: loggingConfig.RollDaily ? RollingInterval.Day : RollingInterval.Infinite,
                        shared: true, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
                }

                // Apply default log level
                logger.MinimumLevel.Is(GetLevel(loggingConfig.Default) ?? LogEventLevel.Information);

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

        public async Task StartAsync()
        {
            if (started)
                throw new InvalidOperationException("The server is already running");
            started = true;

            // Get port
            ushort port;
            try
            {
                port = Configuration.GetValue<ushort>("Port", 32887);
            }
            catch (Exception)
            {
                port = 32887;
            }

            Logger.LogInformation("Loading");

            var sw = new Stopwatch();

            // Load the map
            Logger.LogInformation("Loading map...");

            sw.Start();
            await Task.Run(() => LoadMap(Configuration["MapName"] ?? "classicgen.vxl"));
            sw.Stop();

            Logger.LogInformation("Loaded map (Took {0:0.00} s)", sw.Elapsed.TotalSeconds);

            // Load ENet
            if (!ManagedENet.Started)
            {
                Logger.LogInformation("Loading ENet libraries...");
                ManagedENet.Startup();
            }

            // Create host
            Logger.LogDebug("Creating host...");

            ENetAsyncHost? host = null;
            try
            {
                host = new ENetAsyncHost(new IPEndPoint(IPAddress.Loopback, port), MaxPlayers, 1);
            }
            catch (NullReferenceException)
            {
                Logger.LogCritical("Failed to initialize ENet. Is the library initialized?");
                return;
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Failed to create host");
                return;
            }

            // Important for the clients to be able to connect
            host.CompressWithRangeCoder();

            Logger.LogInformation("Starting host");
            await host.StartAsync();

            Logger.LogInformation("Ready");
            Logger.LogDebug($"Listening on port {port}");

            // TODO: Find a better place for this
            _ = Task.Run(ServerLoop);

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    ENetAsyncPeer peer = await host.AcceptAsync(cts.Token);

                    // Only v0.75 is supported
                    if (peer.ConnectData != 3)
                    {
                        await peer.DisconnectAsync((uint)DisconnectReason.WrongProtocolVersion);
                        Logger.LogInformation($"A client tried to connect with unsupported protocol version: {peer.ConnectData}");
                        return;
                    }

                    if (Clients.Count >= MaxPlayers)
                    {
                        await peer.DisconnectAsync((uint)DisconnectReason.ServerFull);
                        Logger.LogInformation("A client tried to connect but the server was full.");
                        return;
                    }

                    Logger.LogInformation($"Client connected: {peer.RemoteEndPoint}");

                    var client = new Client(this, peer, GetFreeId(Clients.Select(c => c.Value.Id).ToArray()));
                    client.Disconnected += p => Clients.TryRemove(p, out _);

                    Clients.TryAdd(peer, client);

                    _ = Task.Run(client.StartAsync).ContinueWith(t =>
                            Logger.LogError(t.Exception, "An exception occured in a client thread"),
                        TaskContinuationOptions.OnlyOnFaulted);

                    Logger.LogDebug("Clients connected: {0}", Clients.Count);
                }
            }
            catch (OperationCanceledException)
            {
                // The server is stopping
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Network loop threw an unhandled exception");
            }
            finally
            {
                if (!cts.IsCancellationRequested)
                    cts.Cancel();

                // TODO: Disconnect clients

                Logger.LogDebug("Stopping host");
                await host.FlushAsync();
                Logger.LogDebug("All packets flushed");

                await host.StopAsync();

                Logger.LogDebug("Host stopped");

                Logger.LogTrace("Disposing host");
                host.Dispose();

                Logger.LogInformation("Server stopped");
            }
        }

        public async Task StopAsync()
        {
            if (cts.IsCancellationRequested)
                return;
            Logger.LogInformation("Stopping server");
            cts.Cancel();
            await Task.CompletedTask;
        }

        private void LoadMap(string name)
        {
            // Bad
            using (var fs = new FileStream(name, FileMode.Open))
                World = new World(Map.Load(fs), GetLogger<World>());
        }

        private async Task ServerLoop()
        {
            Logger.LogDebug("Starting server loop");

            float tps = Configuration.GetValue<float>("Tps");

            // Milliseconds per tick
            var mspt = TimeSpan.FromMilliseconds(1000f / tps);

            var nextTick = TimeSpan.Zero;
            var sw = Stopwatch.StartNew();

            while (!cts.IsCancellationRequested)
            {
                try
                {
                    await World!.UpdateAsync((float)mspt.TotalSeconds);

                    nextTick = nextTick.Add(mspt);

                    // Sleep until it's time to process a new tick
                    var elapsed = sw.Elapsed;
                    if (elapsed < nextTick)
                        await Task.Delay(nextTick - elapsed);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error in server loop");
                }
            }

            sw.Stop();

            Logger.LogDebug("Server loop stopped");
        }

        internal static byte GetFreeId(byte[] inUse)
        {
            if (inUse.Length == 0)
                return 0;

            byte id = 0;

            foreach (byte lowest in inUse)
            {
                for (; id <= lowest; id++)
                {
                    if (id != lowest)
                        return id;
                }
            }
            return id;
        }

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