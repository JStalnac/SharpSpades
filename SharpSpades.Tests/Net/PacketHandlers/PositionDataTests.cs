using Moq;
using SharpSpades.Api;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Net.PacketHandlers;
using System.Numerics;
using Xunit;

namespace SharpSpades.Tests.Net.PacketHandlers
{
    public class PositionDataTests
    {
        [Fact]
        public async Task Test_NoOp_WhenDead()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.IsAlive)
                .Returns(false);
            
            var handler = new PositionDataHandler();
            
            await handler.HandleAsync(mock.Object, new PositionData());

            mock.VerifyGet(c => c.IsAlive);
            mock.VerifyNoOtherCalls();
        }
        
        [Fact]
        public async Task Test_PositionSet_ValidPosition()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.IsAlive)
                .Returns(true);
            mock.Setup(c => c.Player.Position)
                .Returns(new Vector3(1f, 1f, 60f));
            
            var handler = new PositionDataHandler();
            
            await handler.HandleAsync(mock.Object, new PositionData
            {
                Position = new Vector3(2f, 2f, 60f)
            });

            mock.VerifySet(c => c.Player.Position = new Vector3(2f, 2f, 60f));
        }

        [Fact]
        public async Task Test_PositionNotSet_InvalidPosition()
        {
            await TestWithInvalidData(new Vector3(1f, 1f, 60f), new Vector3(100f, 100f, 60f));
        }

        [Fact]
        public async Task Test_PositionNotSet_Nan()
        {
            await TestWithInvalidData(new Vector3(1f, 1f, 1f), new Vector3(Single.NaN, Single.NaN, Single.NaN));
        }

        private async Task TestWithInvalidData(Vector3 realPosition, Vector3 position)
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.IsAlive)
                .Returns(true);
            mock.Setup(c => c.Player.Position)
                .Returns(realPosition);
            mock.SetupLoggerFor<PositionData>();
            
            var handler = new PositionDataHandler();
            
            await handler.HandleAsync(mock.Object, new PositionData
            {
                Position = position
            });

            mock.VerifyGet(c => c.IsAlive);
            mock.VerifyGet(c => c.Player.Position);
            mock.Verify(c => c.Server.GetLogger<PositionData>());
            
            // Revert position client side
            mock.Verify(c => c.SendPacketAsync(It.IsAny<PositionData>(), It.IsAny<PacketFlags>()));

            mock.VerifyNoOtherCalls();
        }
    }
}