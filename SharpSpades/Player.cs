using SharpSpades.Api;
using SharpSpades.Api.Net;
using SharpSpades.Api.Utils;

namespace SharpSpades
{
    public class Player : IPlayer
    {
        public IClient Client { get; }

        public Player(Client client)
        {
            Throw.IfNull(client, nameof(client));
            Client = client;
        }
    }
}
