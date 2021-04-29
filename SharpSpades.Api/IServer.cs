using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

#nullable enable

namespace SharpSpades.Api
{
    public interface IServer
    {
        IWorld? World { get; }
        ILogger<T> GetLogger<T>();
        ILogger GetLogger(string categoryName);
        Task StartAsync();
        Task StopAsync();
    }
}
