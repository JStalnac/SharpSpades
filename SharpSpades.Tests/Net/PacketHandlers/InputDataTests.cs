using Moq;
using SharpSpades.Api;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Net.PacketHandlers;
using Xunit;

namespace SharpSpades.Tests.Net.PacketHandlers
{
    public class InputDataTests
    {
        [Fact]
        public async Task Test_NoOp_WhenDead()
        {
            var mock = new Mock<IClient>();
            mock.SetupGet(c => c.IsAlive)
                .Returns(false);
            
            var handler = new InputDataHandler();

            await handler.HandleAsync(mock.Object, new InputData
            {
                PlayerId = 0,
                InputState = (InputState)0
            });
            
            mock.VerifyGet(c => c.IsAlive);
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Test_JumpNotSet_WhenPlayerAirborne()
        {
            var mock = new Mock<IClient>();
            mock.SetupGet(c => c.IsAlive)
                .Returns(true);
            mock.SetupGet(c => c.Player.IsAirborne)
                .Returns(true);
            mock.SetupSet(c => c.Player.InputState = (InputState)0);
            
            var handler = new InputDataHandler();

            await handler.HandleAsync(mock.Object, new InputData
            {
                PlayerId = 0,
                InputState = InputState.Up | InputState.Jump
            });

            mock.VerifySet(c => c.Player.InputState = InputState.Up);
        }
    }
}