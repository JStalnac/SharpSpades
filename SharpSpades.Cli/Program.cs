using ENet.Managed;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SharpSpades.Cli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var server = new Server(Directory.GetCurrentDirectory());

                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    server.StopAsync().Wait();
                };

                await server.StartAsync();
            }
            finally
            {
                ManagedENet.Shutdown();
            }
        }
    }
}
