using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SharpSpades.Api
{
    public interface IServer
    {
        public ILogger<T> GetLogger<T>();
        public ILogger GetLogger(string categoryName);
        public Task StartAsync();
        public Task StopAsync();
    }
}
