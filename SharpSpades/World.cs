using Microsoft.Extensions.Logging;
using SharpSpades.Entities;
using SharpSpades.Utils;
using SharpSpades.Vxl;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace SharpSpades
{
    public class World
    {
        public Map Map { get; }

        private HashSet<Entity> Entities { get; } = new();

        private readonly ILogger<World> logger;
        private readonly object entityLock = new();

        internal World(Map map, ILogger<World> logger)
        {
            Map = map;
            this.logger = logger;
        }

        public void AddEntity(Entity entity)
        {
            Throw.IfNull(entity, nameof(entity));

            if (entity.World is not null)
                throw new ArgumentException("The entity is already in a world");

            lock (entityLock)
            {
                Entities.Add(entity);
                entity.World = this;
            }
        }

        public void RemoveEntity(Entity entity)
        {
            Throw.IfNull(entity, nameof(entity));

            lock (entityLock)
            {
                if (!Entities.Remove(entity))
                    throw new ArgumentException("The entity is not in the world");

                entity.World = null;
            }
        }

        public ImmutableArray<Entity> GetEntities()
            => Entities.ToImmutableArray();

        internal async Task UpdateAsync()
        {
            logger.LogTrace("Beginning world update");
            var start = DateTime.Now;

            IEnumerable<Task> tasks;
            lock (entityLock)
                tasks = Entities.Select(e => e.UpdateAsync()).ToArray();

            await Task.WhenAll(tasks);

            foreach (var ex in tasks.Where(t => t.IsFaulted)
                .Select(t => t.Exception))
                logger.LogError(ex, "Failed to update entity");

            logger.LogTrace("World update took {Time:F2} ms", (DateTime.Now - start).TotalMilliseconds);
        }
    }
}