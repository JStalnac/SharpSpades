using SharpSpades.Api.Net.Packets;
using System.Threading.Tasks;

#nullable enable

namespace SharpSpades.Api.Net
{
    public interface IClient
    {
        byte Id { get; }
        public IServer Server { get; }
        IPlayer? Player { get; }

        ValueTask DisconnectAsync(DisconnectReason reason);

        ValueTask SendPacket(IPacket packet);
    }
}