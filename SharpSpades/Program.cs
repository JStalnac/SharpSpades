using ENet.Managed;
using System.Threading.Tasks;

namespace SharpSpades
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var server = new Server(null);
                await server.StartAsync();
            }
            finally
            {
                ManagedENet.Shutdown();
            }
        }
    }
}
