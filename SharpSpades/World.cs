using SharpSpades.Api;
using SharpSpades.Api.Entities;
using SharpSpades.Api.Utils;
using SharpSpades.Entities;
using SharpSpades.Vxl;
using System;
using System.Collections.Generic;

namespace SharpSpades
{
    public class World : IWorld
    {
        public Map Map { get; }

        private HashSet<Entity> Entities { get; } = new();

        private object entityLock = new object();

        internal World(Map map)
        {
            Map = map;
        }

        public void AddEntity(IEntity entity)
        {
            Throw.IfNull(entity);

            if (entity is not Entity e)
                throw new ArgumentException("Invalid entity");

            lock(entityLock)
            {
                Entities.Add(e);
                e.World = this;
            }
        }

        public void RemoveEntity(IEntity entity)
        {
            if (entity is not Entity e)
                throw new ArgumentException("Invalid entity");

            lock(entityLock)
            {
                // Should maybe set the world to null or something
                Entities.Remove(e);
            }
        }
    }
}
