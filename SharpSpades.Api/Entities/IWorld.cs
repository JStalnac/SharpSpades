using SharpSpades.Api.Vxl;
using System.Collections.Immutable;

namespace SharpSpades.Api.Entities
{
    public interface IWorld
    {
        IMap Map { get; }

        void AddEntity(IEntity entity);

        void RemoveEntity(IEntity entity);

        ImmutableArray<IEntity> GetEntities();
    }
}