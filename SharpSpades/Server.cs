using ENet.Managed;
using ENet.Managed.Async;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharpSpades.Api.Entities;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Entities;
using SharpSpades.Net;
using SharpSpades.Plugins;
using SharpSpades.Utils;
using SharpSpades.Vxl;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Timers;

#nullable enable

namespace SharpSpades
{
    public class Server : BackgroundService, IServer
    {
        public IConfiguration Configuration { get; }
        private ILoggerFactory LoggerFactory { get; }
        public PluginManager PluginManager { get; }
        internal ILogger<Server> Logger { get; }

        public ImmutableDictionary<byte, IClient> Clients { get; private set; } = new Dictionary<byte, IClient>().ToImmutableDictionary();

        public World? World { get; private set; }
        IWorld? IServer.World => World;

        private volatile bool started;
        private readonly IServiceProvider serviceProvider;
        private readonly IHostApplicationLifetime applicationLifetime;

        public const short MaxPlayers = 32;

        public Server(IServiceProvider serviceProvider, IHostApplicationLifetime applicationLifetime, IConfiguration configuration, ILoggerFactory loggerFactory, ILogger<Server> logger,
            PluginManager pluginManager)
        {
            Throw.IfNull(configuration);
            Throw.IfNull(applicationLifetime);
            Throw.IfNull(loggerFactory);
            Throw.IfNull(pluginManager);
            Throw.IfNull(logger);
            this.serviceProvider = serviceProvider;
            this.applicationLifetime = applicationLifetime;
            Configuration = configuration;
            LoggerFactory = loggerFactory;
            PluginManager = pluginManager;
            Logger = LoggerFactory.CreateLogger<Server>();
        }

        public ILogger<T> GetLogger<T>()
            => LoggerFactory.CreateLogger<T>();

        public Microsoft.Extensions.Logging.ILogger GetLogger(string categoryName)
            => LoggerFactory.CreateLogger(categoryName);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return;
            
            if (started)
                throw new InvalidOperationException("The server is already running");
            started = true;

            Logger.LogInformation("Loading");

            Logger.LogInformation("Enabling plugins");

            PluginManager.RegisterServiceProvider(serviceProvider);
            foreach (var p in PluginManager.GetPlugins())
                await PluginManager.EnableAsync(p);
            
            Logger.LogInformation("Plugins enabled");

            var sw = new Stopwatch();

            // Load the map
            Logger.LogInformation("Loading map...");

            sw.Start();
            World = new World(await Map.LoadAsync(Configuration["MapName"] ?? "classicgen.vxl"),
                            GetLogger<World>());
            sw.Stop();

            Logger.LogInformation("Loaded map (Took {0:0.00} s)", sw.Elapsed.TotalSeconds);

            _ = ServerLoop(stoppingToken);

            double mspt = 1000d / Configuration.GetValue<ushort>("Ups", 10);
            var timer = new System.Timers.Timer(mspt);
            timer.Elapsed += SendWorldUpdatesAsync;
            timer.Start();

            // Load ENet
            if (!ManagedENet.Started)
            {
                Logger.LogDebug("Loading ENet libraries...");
                ManagedENet.Startup();
            }

            // Create host
            Logger.LogDebug("Creating host...");

            ENetAsyncHost host;
            ushort port = Configuration.GetValue<ushort>("Port", 32887);
            try
            {
                host = new ENetAsyncHost(new IPEndPoint(IPAddress.Loopback, port), MaxPlayers, 1);
            }
            catch (NullReferenceException)
            {
                Logger.LogCritical("Failed to initialize ENet. Is the library initialized?");
                applicationLifetime.StopApplication();
                return;
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Failed to create host. Is the port is use?");
                applicationLifetime.StopApplication();
                return;
            }

            // Important for the clients to be able to connect
            host.CompressWithRangeCoder();

            var clientArray = new (IClient Client, Task Task)?[MaxPlayers];
            // Peer -> Id cache
            var clients = new Dictionary<ENetAsyncPeer, byte>();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void UpdateClients()
            {
                Clients = clientArray
                    .Where(x => x is not null)
                    .Select((x, i) => (x!.Value.Client, (byte)i))
                    .ToImmutableDictionary(k => k.Item2, v => v.Client);
            }

            UpdateClients();

            Logger.LogInformation($"Listening on port {port}");
            await host.StartAsync();

            try
            {
                // Let clients disconnect before shutting down
                while (!stoppingToken.IsCancellationRequested || clients.Count > 0)
                {
                    var peer = await host.AcceptAsync(stoppingToken);

                    Logger.LogDebug("New connection from {Endpoint}", peer.RemoteEndPoint);
                    _ = peer.Disconnection.ContinueWith(task => 
                    {
                        Logger.LogDebug("Peer {Endpoint} disconnected", peer.RemoteEndPoint);
                    }, TaskContinuationOptions.RunContinuationsAsynchronously);

                    // Check if the client can connect
                    // TODO: Maybe move to Client
                    if (peer.ConnectData != 3)
                    {
                        await peer.DisconnectAsync((uint)DisconnectReason.WrongProtocolVersion);
                        Logger.LogInformation("A client tried to connect with unsupported protocol version: {Version}",
                                peer.ConnectData);
                        continue;
                    }

                    if (clients.Count >= MaxPlayers)
                    {
                        Logger.LogInformation("A client tried to connect but the server was full");
                        await peer.DisconnectAsync((uint)DisconnectReason.ServerFull);
                        break;
                    }

                    byte id = 0;
                    for (byte i = 0; i < clientArray.Length; i++)
                    {
                        // We won't get here if the server is full
                        if (clientArray[i] == null)
                        {
                            id = i;
                            break;
                        }
                    }

                    var client = new Client(this, id, peer);

                    Logger.LogDebug("{Endpoint} is {Client}",
                        peer.RemoteEndPoint, client);

                    Logger.LogDebug("Client task for {Client} started", client);
                    var clientTask = client.StartAsync(stoppingToken).ContinueWith(async task =>
                    {
                        // Executes when the client has stopped

                        // Log when the client stops
                        if (task.IsFaulted)
                        {
                            Logger.LogError(task.Exception, "Client task for {Client} threw an unhandled exception",
                                    client);
                        }
                        Logger.LogDebug("Client task for {Client} stopped", client);

                        // Protects against unexpected bugs
                        if (peer.IsConnected)
                        {
                            Logger.LogWarning("{Client} was still connected after the client task had stopped", client);
                            await peer.DisconnectAsync((uint)DisconnectReason.Undefined);
                        }

                        clients.Remove(peer, out byte id);
                        clientArray[id] = null;
                        UpdateClients();
                    });

                    clients.Add(peer, id);
                    clientArray[id] = (client, clientTask);
                    UpdateClients();
                }
            }
            catch (OperationCanceledException) { /* Server stopping */ }

            Logger.LogInformation("Stopping server");

            Logger.LogTrace("Waiting for client tasks to finish processing");
            await Task.WhenAll(clientArray
                .Where(x => x is not null)
                .Select(x => x!.Value.Task));
            Logger.LogTrace("All client tasks stopped");

            Logger.LogDebug("Stopping host...");
            await host.StopAsync();
            Logger.LogTrace("Disposing host");
            host.Dispose();

            Logger.LogTrace("Destroying map");
            World!.Map.Free();

            Logger.LogInformation("Disabling plugins");
            
            foreach (var p in PluginManager.GetPlugins())
                await PluginManager.DisableAsync(p);

            Logger.LogInformation("Disabled plugins");

            Logger.LogInformation("Server stopped");
        }

        /// <summary>
        /// Sends the packet to all connected clients that have completed the map download.
        /// </summary>
        /// <param name="packet">The packet to send</param>
        /// <param name="packetFlags">Packet flags for sending the packet. Defaults to <see cref="PacketFlags.Reliable"/>.</param>
        /// <param name="quiet">Passed to <see cref="Client.SendPacketAsync(Packet, PacketFlags, bool)"/>.</param>
        /// <remarks>Does not throw exceptions that may occur when sending the packet to clients.</remarks>
        /// <returns></returns>
        public async Task BroadcastPacketAsync(IPacket packet, PacketFlags packetFlags = PacketFlags.Reliable)
        {
            Throw.IfNull(packet, nameof(packet));

            var tasks = Clients.Values
                .Where(c => c.IsConnected && c.MapDownloadComplete)
                .Select(c => c.SendPacketAsync(packet, packetFlags));
            foreach (var task in tasks)
                await task;
        }

        private void SendWorldUpdatesAsync(object? source, ElapsedEventArgs e)
        {
            try
            {
                BroadcastPacketAsync(new WorldUpdate(Clients.Values, MaxPlayers),
                            PacketFlags.Unreliable)
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to send world updates");
            }
        }

        private async Task ServerLoop(CancellationToken ct)
        {
            Logger.LogDebug("Starting server loop");

            float tps = Configuration.GetValue<float>("Tps", 60f);

            // Milliseconds per tick
            var mspt = TimeSpan.FromMilliseconds(1000f / tps);

            var nextTick = TimeSpan.Zero;
            var sw = Stopwatch.StartNew();

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await World!.UpdateAsync((float)mspt.TotalSeconds, (float)sw.Elapsed.TotalSeconds);

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
    }
}