using System.Numerics;

namespace SharpSpades.Api.Net.Packets
{
    public struct WorldUpdate : IPacket
    {
        public byte Id => 2;
        public int Length => Players.Length * 24;

        public (Vector3, Vector3)[] Players { get; }

        public WorldUpdate(IEnumerable<IClient> clients, int slots)
        {
            if (slots < 1)
                throw new ArgumentOutOfRangeException(nameof(slots));
            Players = new (Vector3, Vector3)[slots];

            // TODO: Needs refactoring

            foreach (var client in clients)
            {
                Vector3 position;
                Vector3 rotation;
                if (client.Player is not null)
                {
                    position = client.Player.Position;
                    rotation = client.Player.Rotation;
                }
                else
                {
                    position = new Vector3(0, 0, 0);
                    rotation = new Vector3(0, 0, 0);
                }
                Players[client.Id] = (position, rotation);
            }
        }

        public void Read(ReadOnlySpan<byte> buffer)
            => throw new NotImplementedException();
        
        public void Write(Span<byte> buffer)
        {
            for (int i = 0; i < Players.Length; i++)
            {
                buffer.WritePosition(Players[i].Item1, i * 24);
                buffer.WritePosition(Players[i].Item2, i * 24 + 12);
            }
        }
    }
}
