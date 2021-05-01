using SharpSpades.Net;
using SharpSpades.Utils;

namespace SharpSpades.Events
{
    public class ClientEventArgs : BaseEventArgs
    {
        public Client Client { get; }

        public ClientEventArgs(Client client)
        {
            Throw.IfNull(client, nameof(client));
            Client = client;
        }
    }
}
