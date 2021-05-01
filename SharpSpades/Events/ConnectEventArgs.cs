using SharpSpades.Net;

namespace SharpSpades.Events
{
    public class ConnectEventArgs : ClientEventArgs
    {
        public ConnectEventArgs(Client client) : base(client) { }
    }
}
