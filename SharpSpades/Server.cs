using ENet.Managed;
using Microsoft.Extensions.Logging;
using SharpSpades.Api;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace SharpSpades
{
    public class Server : IServer
    {
        public ILoggerFactory LoggerFactory { get; }
        private readonly ILogger<Server> logger;
        internal readonly CancellationTokenSource cts = new();

        public Server(string? configurationDirectory, Action<ILoggingBuilder> configureLogging)
        {
            if (configureLogging is null)
                throw new ArgumentNullException(nameof(configureLogging));

            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(c =>
            {
                try
                {
                    configureLogging(c);
                }
                catch (Exception ex)
                {
                    // Can't write to the logger
                    Console.WriteLine($"Error while configuring logging:\n{ex}");
                }
            });
            logger = LoggerFactory.CreateLogger<Server>();
        }

        public ILogger<T> GetLogger<T>()
            => LoggerFactory.CreateLogger<T>();

        public ILogger GetLogger(string categoryName)
            => LoggerFactory.CreateLogger(categoryName);

        public Task StartAsync()
        {
            ushort port = 32887;

            logger.LogInformation("Loading");
            if (!ManagedENet.Started)
            {
                logger.LogInformation("Loading ENet libraries...");
                ManagedENet.Startup();
            }

            var listenEndPoint = new IPEndPoint(IPAddress.Loopback, port);

            logger.LogDebug("Creating host...");
            ENetHost host;
            try
            {
                host = new ENetHost(listenEndPoint, 1, 1);
            }
            catch (NullReferenceException)
            {
                logger.LogCritical("Failed to initialize ENet. Is the library initialized?");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Failed to create host");
                return Task.CompletedTask;
            }

            // Important for the clients to be able to connect
            host.CompressWithRangeCoder();

            logger.LogInformation($"Ready");
            logger.LogDebug($"Listening on port {port}");

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
                            logger.LogInformation($"Peer connected: {ev.Peer.GetRemoteEndPoint()}");
                            if (ev.Data != 3)
                                // Only v0.75 is supported
                                ev.Peer.Disconnect((uint)DisconnectReason.WrongProtocolVersion);
                            continue;
                        case ENetEventType.Disconnect:
                            logger.LogInformation($"Peer disconnected: {ev.Peer.GetRemoteEndPoint()}");
                            continue;
                        case ENetEventType.Receive:
                            logger.LogInformation($"Received data: {ev.Peer.GetRemoteEndPoint()}: {string.Join("", ev.Packet.Data.ToArray())}");
                            ev.Packet.Destroy();
                            continue;
                    }
                }
                
            }
            finally
            {
                logger.LogDebug("Disposing host");
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
    }
}
