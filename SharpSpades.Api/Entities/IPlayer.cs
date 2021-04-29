using SharpSpades.Api.Net;

namespace SharpSpades.Api.Entities
{
    public interface IPlayer : IEntity
    {
        IClient Client { get; }
    }
}