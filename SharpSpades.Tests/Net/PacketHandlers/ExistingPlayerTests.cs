using Moq;
using SharpSpades.Api;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Net.PacketHandlers;
using System.Numerics;
using Xunit;

namespace SharpSpades.Tests.Net.PacketHandlers
{
    public class ExistingPlayerTests
    {
        [Fact]
        public async Task Test_Spawn_WhenInLimbo()
        {
            var mock = new Mock<IClient>();
            mock.SetupGet(c => c.Server)
                .Returns(Mock.Of<IServer>());
            mock.SetupGet(c => c.IsInLimbo)
                .Returns(true);
            
            var handler = new ExistingPlayerHandler();

            await handler.HandleAsync(mock.Object, new ExistingPlayer
            {
                Name = "Deuce"
            });

            mock.Verify(c => c.SpawnAsync(It.IsAny<Vector3>()));
        }

        [Fact]
        public async Task Test_Disconnect_WithInvalidName()
        {
            var mock = new Mock<IClient>();
            mock.SetupGet(c => c.IsInLimbo)
                .Returns(true);
            
            var handler = new ExistingPlayerHandler();

            await handler.HandleAsync(mock.Object, new ExistingPlayer
            {
                Name = "This name is way too long"
            });

            mock.VerifyGet(c => c.IsInLimbo);
            mock.Verify(c => c.DisconnectAsync(It.IsAny<DisconnectReason>()));
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Test_Spawn_NullInName()
        {
            var mock = new Mock<IClient>();
            mock.SetupGet(c => c.Server)
                .Returns(Mock.Of<IServer>());
            mock.Setup(c => c.IsInLimbo)
                .Returns(true);
            
            var handler = new ExistingPlayerHandler();

            await handler.HandleAsync(mock.Object, new ExistingPlayer
            {
                Name = "NameWithNull\0\0"
            });

            mock.Verify(c => c.SpawnAsync(It.IsAny<Vector3>()));
        }

        [Fact]
        public async Task Test_NoSpawn_AlreadyInGame()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.IsInLimbo)
                .Returns(false);
            mock.Setup(c => c.Team)
                .Returns(TeamType.Blue);
            mock.SetupLoggerFor<ExistingPlayer>();
            
            var handler = new ExistingPlayerHandler();

            await handler.HandleAsync(mock.Object, new ExistingPlayer());

            mock.VerifyGet(c => c.IsInLimbo);
            mock.VerifyGet(c => c.Team);
            mock.Verify(c => c.Server.GetLogger<ExistingPlayer>());
            mock.VerifyNoOtherCalls();
        }
        
        [Fact]
        public async Task Test_Spawn_WhenSpectator()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.IsInLimbo)
                .Returns(false);
            mock.Setup(c => c.Team)
                .Returns(TeamType.Spectator);
            mock.Setup(c => c.Server)
                .Returns(Mock.Of<IServer>());
            
            var handler = new ExistingPlayerHandler();

            await handler.HandleAsync(mock.Object, new ExistingPlayer
            {
                Name = "Deuce"
            });

            mock.Verify(c => c.SpawnAsync(It.IsAny<Vector3>()));
        }
    }
}