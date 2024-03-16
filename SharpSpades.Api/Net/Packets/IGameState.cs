namespace SharpSpades.Api.Net.Packets
{
    public interface IGameState
    {
        void WriteTo(Span<byte> ms);

        int Length { get; }
    }
}