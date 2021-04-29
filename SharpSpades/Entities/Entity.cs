using SharpSpades.Api;
using SharpSpades.Api.Entities;
using System.Numerics;
using System.Threading.Tasks;

namespace SharpSpades.Entities
{
    public class Entity : IEntity
    {
        public IWorld World { get; internal set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }

        internal virtual Task UpdateAsync(float delta) => Task.CompletedTask;
    }
}
