using ENet.Managed;
using System.IO;
using System.Threading.Tasks;

namespace SharpSpades
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var server = new Server(Directory.GetCurrentDirectory());
                await server.StartAsync();
            }
            finally
            {
                ManagedENet.Shutdown();
            }
        }
    }
}
