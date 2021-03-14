using System.IO;

namespace SharpSpades.Api.Net.Packets
{
    public interface IPacket
    {
        public byte Id { get; }

        public void Read(MemoryStream ms);
        public void WriteTo(MemoryStream ms);
    }
}
