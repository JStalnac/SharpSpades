using SharpSpades.Api.Net;

namespace SharpSpades.Api
{
    public interface IPlayer
    {
        IClient Client { get; }
    }
}