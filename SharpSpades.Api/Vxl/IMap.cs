using System.Numerics;

namespace SharpSpades.Api.Vxl
{
    public interface IMap
    {
        /// <summary>
        /// Casts a ray with the specified length in the world from the specified position to the specified direction.
        /// Sets <paramref name="hit"/> if the ray intersects with a block.
        /// </summary>
        /// <param name="map">The map to send the ray in.</param>
        /// <param name="position">The position to send the ray from.</param>
        /// <param name="orientation">The direction to send the ray.</param>
        /// <param name="length">The length of the ray to send.</param>
        /// <param name="hit">Optionally set if the ray intersects with a block.</param>
        /// <returns>True if the ray intersects with a block, else false.</returns>
        bool CastRay(Vector3 position, Vector3 orientation, float length, out Vector3? hit);
    }
}