using SharpSpades.Api.Entities;
using SharpSpades.Vxl;

namespace SharpSpades.Api
{
    public interface IWorld
    {
        /// <summary>
        /// The game map
        /// </summary>
        Map Map { get; }

        /// <summary>
        /// Adds an entity to the world.
        /// </summary>
        /// <param name="entity">The entity to be added.</param>
        void AddEntity(IEntity entity);
        /// <summary>
        /// Removes an entity from the world.
        /// </summary>
        /// <param name="entity"></param>
        void RemoveEntity(IEntity entity);
    }
}
