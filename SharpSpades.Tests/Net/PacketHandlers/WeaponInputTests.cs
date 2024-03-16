using Moq;
using SharpSpades.Api.Net;
using SharpSpades.Api.Net.Packets;
using SharpSpades.Net.PacketHandlers;
using Xunit;

namespace SharpSpades.Tests.Net.PacketHandlers
{
    public class WeaponInputTests
    {
        [Fact]
        public async Task Test_Ignore_WhenDead()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.IsAlive)
                .Returns(false);
            
            var handler = new WeaponInputHandler();

            // Should not throw
            await handler.HandleAsync(mock.Object, new WeaponInput());
        }

        [Fact]
        public async Task Test_InputSet()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.IsAlive)
                .Returns(true);
            mock.SetupProperty(c => c.Player.PrimaryFire);
            mock.SetupProperty(c => c.Player.SecondaryFire);
            TestHelpers.SetupSendToOthers(mock);
            
            var handler = new WeaponInputHandler();

            await handler.HandleAsync(mock.Object, new WeaponInput
            {
                PrimaryFire = true,
                SecondaryFire = true
            });

            mock.VerifySet(c => c.Player.PrimaryFire = true);
            mock.VerifySet(c => c.Player.SecondaryFire = true);
        }
    }
}