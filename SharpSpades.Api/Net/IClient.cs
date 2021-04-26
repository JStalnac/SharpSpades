using SharpSpades.Api.Entities;
using SharpSpades.Api.Net.Packets;
using System.Threading.Tasks;

#nullable enable

// We may want to remove this interface and just have IPlayer
// available in the api as this doesn't really serve a purpose
// and it adds complexity.

namespace SharpSpades.Api.Net
{
    public interface IClient
    {
        byte Id { get; }
        IServer Server { get; }
        IPlayer? Player { get; }

        ValueTask DisconnectAsync(DisconnectReason reason);

        ValueTask SendPacket(IPacket packet);
    }
}