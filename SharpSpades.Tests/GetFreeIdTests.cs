using NUnit.Framework;
using SharpSpades;
using System;

namespace Tests
{
    public class GetFreeIdTests
    {
        [Test]
        public void GetFreeId_NoIds()
        {
            Assert.AreEqual(0, Server.GetFreeId(Array.Empty<byte>()));
        }

        [Test]
        public void GetFreeId_Test1()
        {
            Assert.AreEqual(1, Server.GetFreeId(new byte[] { 0 }));
        }

        [Test]
        public void GetFreeId_Test2()
        {
            Assert.AreEqual(2, Server.GetFreeId(new byte[] { 0, 1 }));
        }

        [Test]
        public void GetFreeId_Test3()
        {
            Assert.AreEqual(2, Server.GetFreeId(new byte[] { 0, 1, 5 }));
        }
    }
}