using Moq;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Net.PacketHandlers;
using System.Collections.Immutable;
using Xunit;

namespace SharpSpades.Tests.Net.PacketHandlers
{
    public class HitPacketTests
    {
        private static void SetupClients(Mock<IClient> mock, IClient target)
        {
            mock.Setup(c => c.Server.Clients)
                .Returns(new Dictionary<byte, IClient>()
                {
                    { 0, mock.Object },
                    { 1, target }
                }.ToImmutableDictionary());
        }

        [Fact]
        public async Task Test_Ignore_WhenDead()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.IsAlive)
                .Returns(false);
            
            var handler = new HitPacketHandler();

            await handler.HandleAsync(mock.Object, new HitPacket());

            mock.VerifyGet(c => c.IsAlive);
            mock.VerifyNoOtherCalls();
        }
        
        [Fact]
        public async Task Test_Ignore_WhenTargetDead()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.IsAlive)
                .Returns(true);
            SetupClients(mock, Mock.Of<IClient>(c =>
                c.IsAlive == false && c.Id == 1));
            
            var handler = new HitPacketHandler();

            await handler.HandleAsync(mock.Object, new HitPacket
            {
                Target = 1
            });

            // Target search
            mock.VerifyGet(c => c.Id);

            mock.VerifyGet(c => c.IsAlive);
            mock.VerifyGet(c => c.Server.Clients);
            
            mock.VerifyNoOtherCalls();
        }
        
        [Fact]
        public async Task Test_Ignore_WhenNotShooting()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.IsAlive)
                .Returns(true);
            mock.Setup(c => c.Player.PrimaryFire)
                .Returns(false);
            SetupClients(mock, Mock.Of<IClient>(c => 
                c.IsAlive == true && c.Id == 1));
            mock.SetupLoggerFor<HitPacket>();
            
            var handler = new HitPacketHandler();

            await handler.HandleAsync(mock.Object, new HitPacket
            {
                Target = 1
            });

            mock.VerifyGet(c => c.Id);
            mock.VerifyGet(c => c.IsAlive);
            mock.VerifyGet(c => c.Player.PrimaryFire);
            mock.VerifyGet(c => c.Server.Clients);
            mock.Verify(c => c.Server.GetLogger<HitPacket>());
            mock.VerifyNoOtherCalls();
        }
    }
}