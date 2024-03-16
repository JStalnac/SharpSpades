using Moq;
using SharpSpades.Api;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Net.PacketHandlers;
using Xunit;

namespace SharpSpades.Tests.Net.PacketHandlers
{
    public class SetToolTests
    {
        [Fact]
        public async Task Test_Ignore_WhenDead()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.IsAlive)
                .Returns(false);
            
            var handler = new SetToolHandler();

            await handler.HandleAsync(mock.Object, new SetTool
            {
                Tool = Tool.Gun
            });

            mock.VerifyGet(c => c.IsAlive);
            mock.VerifyNoOtherCalls();
        }
        
        [Fact]
        public async Task Test_Ignore_InvalidTool()
        {
            var mock = new Mock<IClient>();
            
            var handler = new SetToolHandler();

            await handler.HandleAsync(mock.Object, new SetTool
            {
                Tool = (Tool)10
            });

            mock.VerifyNoOtherCalls();
        }
        
        [Fact]
        public async Task Test_ToolSet_Valid()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.IsAlive)
                .Returns(true);
            mock.SetupProperty(c => c.Player.Tool);
            TestHelpers.SetupSendToOthers(mock);
            
            var handler = new SetToolHandler();

            await handler.HandleAsync(mock.Object, new SetTool
            {
                Tool = Tool.Block
            });
            
            mock.VerifySet(c => c.Player.Tool = Tool.Block);
        }
    }
}