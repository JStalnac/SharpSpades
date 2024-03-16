using SharpSpades.Api.Entities;
using System.Numerics;

namespace SharpSpades.Entities
{
    public class Entity : IEntity
    {
        public World World { get; set; }
        IWorld IEntity.World
        {
            get => World;
            set => World = (World)value;
        }

        public virtual Vector3 Position { get; set; }

        public virtual Vector3 Rotation { get; set; }

        public virtual Task UpdateAsync(float delta, float time) => Task.CompletedTask;
    }
}