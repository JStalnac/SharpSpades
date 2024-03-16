using System.Numerics;

namespace SharpSpades.Api.Entities
{
    public interface IEntity
    {
        IWorld World { get; set; }

        Vector3 Position { get; set; }

        Vector3 Rotation { get; set; }

        Task UpdateAsync(float delta, float time);
    }
}