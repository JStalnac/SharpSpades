using SharpSpades.Utils;
using Xunit;

namespace SharpSpades.Tests
{
    public class NameUtilsTests
    {
        [Theory]
        [InlineData("Deuce")]
        [InlineData("s p a c e s")]
        [InlineData("123456789ABCDEF")]
        public void TestValid(string name)
        {
            Assert.True(NameUtils.IsValidName(name));
        }

        [Theory]
        [InlineData("Null\x0")]
        [InlineData("123456789ABCDEF1")]
        public void TestInvalid(string name)
        {
            Assert.False(NameUtils.IsValidName(name));
        }
    }
}