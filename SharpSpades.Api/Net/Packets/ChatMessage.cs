namespace SharpSpades.Api.Net.Packets;

public struct ChatMessage : IPacket
{
    /// <summary>
    /// Maximum length for message sent with the packet. Messages longer than this will be cut short when writing packet.
    /// The user has to implement logic for longer messages.
    /// </summary>
    // Piqueserver uses this value, it may be higher though
    // TODO: Find what the actual max length is
    public const int MaxMessageLength = 90;

    public byte Id => 17;

    public int Length => 2 + Math.Min(rawMessage.Length, MaxMessageLength);

    /// <summary>
    /// The id of the player who sent the message
    /// </summary>
    /// <value></value>
    public byte Sender { get; set; }

    /// <summary>
    /// Who the message is directed to. See <see cref="ChatType" />.
    /// </summary>
    /// <value></value>
    public ChatType Type { get; set; }

    public string Message
    {
        get => message;
        set
        {
            rawMessage = StringUtils.ToCP437String(value);
            message = value;
        }
    }

    private string message;
    private Memory<byte> rawMessage;

    /// <summary>
    /// Creates a Chat Message packet that has Type <see cref="ChatType.System" />.
    /// </summary>
    /// <param name="message"></param>
    public ChatMessage(string message)
    {
        Sender = 0;
        Type = ChatType.System;
        rawMessage = StringUtils.ToCP437String(message);
        this.message = message;
    }

    /// <summary>
    /// Creates a Chat Message packet with the given values.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="type"></param>
    /// <param name="message"></param>
    public ChatMessage(byte id, ChatType type, string message)
    {
        Sender = id;
        Type = type;
        rawMessage = StringUtils.ToCP437String(message);
        this.message = message;
    }

    public void Read(ReadOnlySpan<byte> buffer)
    {
        Sender = buffer[0];
        Type = (ChatType)buffer[1];
        Message = StringUtils.ReadCP437String(buffer.Slice(2));
    }

    public void Write(Span<byte> buffer)
    {
        buffer[0] = Sender;
        buffer[1] = (byte)Type;
        // Limit the length of the message
        rawMessage.Span.Slice(0, Math.Min(rawMessage.Length, MaxMessageLength))
            .CopyTo(buffer.Slice(2));
    }
}