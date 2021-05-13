using System;
using Xunit;

namespace SharpSpades.Tests
{
    public class GetFreeIdTests
    {
        [Fact]
        public void GetFreeId_NoIds()
        {
            Assert.Equal(0, Server.GetFreeId(Array.Empty<byte>()));
        }

        [Fact]
        public void GetFreeId_Test1()
        {
            Assert.Equal(1, Server.GetFreeId(new byte[] { 0 }));
        }

        [Fact]
        public void GetFreeId_Test2()
        {
            Assert.Equal(2, Server.GetFreeId(new byte[] { 0, 1 }));
        }

        [Fact]
        public void GetFreeId_Test3()
        {
            Assert.Equal(2, Server.GetFreeId(new byte[] { 0, 1, 5 }));
        }
    }
}