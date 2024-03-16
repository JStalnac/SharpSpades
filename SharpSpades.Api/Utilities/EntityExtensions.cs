using SharpSpades.Api.Entities;

namespace SharpSpades.Api.Utilities
{
    public static class EntityExtensions
    {
        /// <summary>
        /// Removes the entity from its world.
        /// </summary>
        /// <param name="entity"></param>
        public static void Remove(this IEntity entity)
        {
            entity.World.RemoveEntity(entity);
        }
    }
}