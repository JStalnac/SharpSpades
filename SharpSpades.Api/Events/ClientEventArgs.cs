using SharpSpades.Api.Utils;
using SharpSpades.Api.Net;

namespace SharpSpades.Api.Events
{
    public class ClientEventArgs : BaseEventArgs
    {
        public IClient Client { get; }

        public ClientEventArgs(IClient client)
        {
            Throw.IfNull(client, nameof(client));
            Client = client;
        }
    }
}
