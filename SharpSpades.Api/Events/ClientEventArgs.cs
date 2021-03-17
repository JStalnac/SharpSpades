using SharpSpades.Api.Net;
using SharpSpades.Api.Utils;

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
