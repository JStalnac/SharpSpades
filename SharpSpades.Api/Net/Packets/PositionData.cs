using System.Numerics;

namespace SharpSpades.Api.Net.Packets
{
    public partial struct PositionData
    {
        public PositionData(Vector3 position)
        {
            Position = position;
        }
    }
}