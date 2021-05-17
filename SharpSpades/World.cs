using SharpSpades.Entities;
using SharpSpades.Utils;
using SharpSpades.Vxl;
using System;
using System.Collections.Generic;

namespace SharpSpades
{
    public class World
    {
        public Map Map { get; }

        private HashSet<Entity> Entities { get; } = new();

        private object entityLock = new();

        internal World(Map map) => this.Map = map;

        public void AddEntity(Entity entity)
        {
            Throw.IfNull(entity, new NullReferenceException($"The {nameof(entity)} cannot be null!"));

            lock(entityLock)
            {
                this.Entities.Add(entity);

                entity.World = this;
            }
        }

        public void RemoveEntity(Entity entity)
        {
            lock(entityLock)
            {
                // Should maybe set the world to null or something
                this.Entities.Remove(entity);
            }
        }
    }
}
