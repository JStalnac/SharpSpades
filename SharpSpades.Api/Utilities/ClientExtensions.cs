using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;

namespace SharpSpades.Api.Utilities
{
    public static class ClientExtensions
    {
        public static async ValueTask SendToOthersAsync(this IClient client, IPacket packet)
        {
            ArgumentNullException.ThrowIfNull(packet);

            var tasks = client.Server.Clients.Values
                    .Where(c => c.IsConnected && c.MapDownloadComplete)
                    .Where(c => c.Id != client.Id)
                    .Select(c => c.SendPacketAsync(packet));
            foreach (var task in tasks)
                await task;
        }
    }
}