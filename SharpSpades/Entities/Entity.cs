using System.Numerics;
using System.Threading.Tasks;

namespace SharpSpades.Entities
{
    public class Entity
    {
        public World World { get; internal set; }

        public Vector3 Position { get; set; }

        public Quaternion Rotation { get; set; }

        internal virtual Task UpdateAsync(float delta) => Task.CompletedTask;
    }
}
