namespace SharpSpades.Api;

/// <summary>
/// Represents the type of a chat message.
/// </summary>
public enum ChatType
{
    /// <summary>
    /// The message a global chat, everyone can see the message.
    /// </summary>
    All,
    /// <summary>
    /// The message a team message, everyone in the players team can see the message.
    /// </summary>
    Team,
    /// <summary>
    /// The message is a system message, these are not sent by a player, but work as informational messages that the server can send.
    System
}