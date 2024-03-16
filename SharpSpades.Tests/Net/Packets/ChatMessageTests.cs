using SharpSpades.Api.Net.Packets;
using Xunit;

namespace SharpSpades.Tests.Net.Packets;

public class ChatMessageTests
{
    [Fact]
    public void Test_CorrectMessage()
    {
        string msg = new string('A', ChatMessage.MaxMessageLength - 10);
        var packet = new ChatMessage(msg);

        Assert.Equal(2 + msg.Length, packet.Length);
    }

    [Fact]
    public void Test_MessageTooLong()
    {
        var packet = new ChatMessage(new string('A', 100));

        Assert.Equal(2 + ChatMessage.MaxMessageLength, packet.Length);
    }
}