using System.Numerics;
using System.Threading.Tasks;

namespace SharpSpades.Entities
{
    public class Entity
    {
        public World World { get; internal set; }

        public virtual Vector3 Position { get; set; }

        public virtual Vector3 Rotation { get; set; }

        public virtual void Remove()
        {
            World.RemoveEntity(this);
        }

        internal virtual Task UpdateAsync(float delta, float time) => Task.CompletedTask;
    }
}