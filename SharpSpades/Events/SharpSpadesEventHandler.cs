using Microsoft.Extensions.Logging;
using SharpSpades.Api.Events;
using System;
using System.Threading.Tasks;

namespace SharpSpades.Events
{
    public class SharpSpadesEventHandler
    {
        private readonly AsyncEvent<ConnectEventArgs> connect;
        
        public SharpSpadesEventHandler(ILogger<SharpSpadesEventHandler> logger)
        {
            connect = new AsyncEvent<ConnectEventArgs>(HandleException, "Connect");
        }

        public event AsyncEventHandler<ConnectEventArgs> Connect
        {
            add => connect.Register(value);
            remove => connect.Unregister(value);
        }

        internal async Task<ConnectEventArgs> InvokeConnect(ConnectEventArgs args)
        {
            await connect.InvokeAsync(args);
            return args;
        }

        private void HandleException(string name, Exception ex) { }
    }
}
