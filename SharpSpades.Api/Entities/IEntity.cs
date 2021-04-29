using System.Numerics;

namespace SharpSpades.Api.Entities
{
    public interface IEntity
    {
        Vector3 Position { get; set; }
        Quaternion Rotation { get; set; }
        IWorld World { get; }
    }
}
