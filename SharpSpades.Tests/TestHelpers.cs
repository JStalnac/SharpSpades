using Microsoft.Extensions.Logging;
using Moq;
using SharpSpades.Api.Net;
using System.Collections.Immutable;

namespace SharpSpades.Tests
{
    public static class TestHelpers
    {
        public static ILoggerFactory CreateLoggerFactory()
            => LoggerFactory.Create(c => c.ClearProviders());

        public static ILogger<T> CreateLogger<T>()
            => CreateLoggerFactory().CreateLogger<T>();
        
        public static void SetupLoggerFor<T>(this Mock<IClient> mock)
        {
            mock.Setup(c => c.Server.GetLogger<T>())
                .Returns(CreateLogger<T>());
        }

        public static void SetupSendToOthers(Mock<IClient> mock)
        {
            mock.Setup(c => c.Server.Clients)
                .Returns(ImmutableDictionary.Create<byte, IClient>());
        }
    }
}