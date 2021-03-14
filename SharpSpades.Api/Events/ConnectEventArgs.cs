using SharpSpades.Api.Net;

namespace SharpSpades.Api.Events
{
    public class ConnectEventArgs : ClientEventArgs
    {
        public ConnectEventArgs(IClient client) : base(client) { }
    }
}
