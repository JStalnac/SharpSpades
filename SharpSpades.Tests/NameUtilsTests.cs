using NUnit.Framework;
using SharpSpades.Utils;

namespace SharpSpades.Tests
{
    public class NameUtilsTests
    {
        [Test]
        public void TestNameUtils()
        {
            Assert.True(NameUtils.IsValidName("Deuce"));
            Assert.True(NameUtils.IsValidName("s p a c e s"));
            Assert.False(NameUtils.IsValidName("Null\x0"));
            Assert.True(NameUtils.IsValidName("123456789ABCDEF"));
            Assert.False(NameUtils.IsValidName("123456789ABCDEF1"));
        }
    }
}