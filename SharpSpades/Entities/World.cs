using Microsoft.Extensions.Logging;
using SharpSpades.Api.Entities;
using SharpSpades.Api.Vxl;
using SharpSpades.Utils;
using SharpSpades.Vxl;
using System.Collections.Immutable;

namespace SharpSpades.Entities
{
    public class World : IWorld
    {
        public Map Map { get; }
        IMap IWorld.Map => Map;

        private ImmutableArray<IEntity> entities = ImmutableArray<IEntity>.Empty;

        private readonly ILogger<World> logger;
        private readonly object entityLock = new();

        internal World(Map map, ILogger<World> logger)
        {
            Map = map;
            this.logger = logger;
        }

        public void AddEntity(IEntity entity)
        {
            Throw.IfNull(entity);

            if (entity.World is not null)
                throw new ArgumentException("The entity is already in a world");
            
            // Try to mitigate race condition by setting World earlier
            entity.World = this;

            lock (entityLock)
            {
                var hashSet = new HashSet<IEntity>(entities);
                logger.LogDebug("Adding {Entity} to world", entity.GetType().Name);
                logger.LogDebug("Entities in world: {Count}", hashSet.Count);
                hashSet.Add(entity);

                entities = hashSet.ToImmutableArray();
            }
        }

        public void RemoveEntity(IEntity entity)
        {
            Throw.IfNull(entity);

            lock (entityLock)
            {
                var hashSet = new HashSet<IEntity>(entities);
                if (!hashSet.Remove(entity))
                    throw new ArgumentException("The entity is not in the world");
                entities = hashSet.ToImmutableArray();

                logger.LogDebug("Removing {Entity} from world", entity.GetType().Name);
                logger.LogDebug("Entities in world: {Count}", hashSet.Count);
                entity.World = null;
            }
        }

        public ImmutableArray<IEntity> GetEntities()
            => entities;

        internal async Task UpdateAsync(float delta, float time)
        {
            logger.LogTrace("Beginning world update");
            var start = DateTime.Now;

            Task[] tasks = entities.Select(e => e.UpdateAsync(delta, time)).ToArray();

            await Task.WhenAll(tasks);

            foreach (var ex in tasks.Where(t => t.IsFaulted)
                .Select(t => t.Exception))
                logger.LogError(ex, "Failed to update entity");

            logger.LogTrace("World update took {Time:F2} ms", (DateTime.Now - start).TotalMilliseconds);
        }
    }
}