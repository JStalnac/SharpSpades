using System.IO;

namespace SharpSpades.Api.Net.Packets.State
{
    public interface IGameState
    {
        public void WriteTo(MemoryStream ms);
    }
}
