using SharpSpades.Entities;
using SharpSpades.Utils;
using SharpSpades.Vxl;
using System.Collections.Generic;

namespace SharpSpades
{
    public class World
    {
        public Map Map { get; }

        private HashSet<Entity> Entities { get; } = new();

        private object entityLock = new object();

        internal World(Map map)
        {
            Map = map;
        }

        public void AddEntity(Entity entity)
        {
            Throw.IfNull(entity);

            lock(entityLock)
            {
                Entities.Add(entity);
                entity.World = this;
            }
        }

        public void RemoveEntity(Entity entity)
        {
            lock(entityLock)
            {
                // Should maybe set the world to null or something
                Entities.Remove(entity);
            }
        }
    }
}
