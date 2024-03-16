using Moq;
using SharpSpades.Api.Entities;
using SharpSpades.Entities;
using SharpSpades.Vxl;
using System.Reflection;
using Xunit;

namespace SharpSpades.Tests
{
    public class WorldTests
    {
        private static World CreateStubWorld()
        {
            var ctor = typeof(Map).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.GetParameters().Length == 1)
                .First();
            var map = (Map)ctor.Invoke(new [] { (object)IntPtr.Zero });
            return new(map, TestHelpers.CreateLogger<World>());
        }

        private static IEntity CreateStubEntity()
        {
            var mock = new Mock<IEntity>();
            mock.SetupProperty(e => e.World);
            return mock.Object;
        }

        [Fact]
        public void Test_AddEntity_EntityAdded()
        {
            var world = CreateStubWorld();
            var entity = CreateStubEntity();

            world.AddEntity(entity);

            Assert.Single(world.GetEntities());
        }

        [Fact]
        public void Test_AddEntity_Null()
        {
            var world = CreateStubWorld();

            Assert.Throws<ArgumentNullException>(() => world.AddEntity(null));
        }

        [Fact]
        public void Test_AddEntity_WorldGetsAssigned()
        {
            var world = CreateStubWorld();
            var entity = CreateStubEntity();

            world.AddEntity(entity);

            Assert.NotNull(entity.World);
        }

        [Fact]
        public void Test_AddEntity_AlreadyInWorld()
        {
            var world = CreateStubWorld();
            var entity = CreateStubEntity();
            entity.World = world;

            Assert.Throws<ArgumentException>(() => world.AddEntity(entity));
        }

        [Fact]
        public void Test_RemoveEntity_EntityRemoved()
        {
            var world = CreateStubWorld();
            var entity = CreateStubEntity();
            world.AddEntity(entity);

            world.RemoveEntity(entity);

            Assert.Empty(world.GetEntities());
        }

        [Fact]
        public void Test_RemoveEntity_WorldUnassigned()
        {
            var world = CreateStubWorld();
            var entity = CreateStubEntity();
            world.AddEntity(entity);

            entity.World = world;

            world.RemoveEntity(entity);

            Assert.Null(entity.World);
        }

        [Fact]
        public void Test_RemoveEntity_EntityNotInWorld()
        {
            var world = CreateStubWorld();
            var entity = CreateStubEntity();

            Assert.Throws<ArgumentException>(() => world.RemoveEntity(entity));
        }
    }
}