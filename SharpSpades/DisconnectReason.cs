namespace SharpSpades
{
    public enum DisconnectReason
    {
        Undefined = 0,
        Banned = 1,
        IPConnectionLimitExceeded = 2,
        WrongProtocolVersion = 3,
        ServerFull = 4,
        Kicked = 10
    }
}
