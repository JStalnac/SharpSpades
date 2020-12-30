namespace SharpSpades.Api
{
    public enum DisconnectReason
    {
        Banned = 1,
        IPConnectionLimitExceeded = 2,
        WrongProtocolVersion = 3,
        ServerFull = 4,
        Kicked = 10
    }
}
