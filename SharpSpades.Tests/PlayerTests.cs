using Moq;
using SharpSpades.Api;
using SharpSpades.Api.Net;
using SharpSpades.Entities;
using Xunit;

namespace SharpSpades.Tests
{
    public class PlayerTests
    {
        [Theory]
        [InlineData(100, 50, 50)]
        [InlineData(100, 99, 1)]
        [InlineData(100, 0, 255)]
        [InlineData(0, 0, 10)]
        [InlineData(50, 100, -50)]
        public void Test_ApplyDamage(byte initial, byte expected, int amount)
        {
            var mock = new Mock<IClient>();
            mock.SetupLoggerFor<Player>();
            mock.SetupGet(c => c.Name)
                .Returns("Deuce");
            mock.SetupGet(c => c.Team)
                .Returns(TeamType.Blue);
            mock.SetupGet(c => c.Weapon)
                .Returns(WeaponType.Rifle);
            
            var player = new Player(mock.Object);
            player.Health = initial;

            player.ApplyDamage(amount);

            Assert.Equal(expected, player.Health);
        }
    }
}