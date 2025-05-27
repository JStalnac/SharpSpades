namespace SharpSpades.Supervisor

open SharpSpades

type OnClientConnect =
    { Version : ProtocolVersion }
    interface IEvent

type OnClientDisconnect =
    { Reason : DisconnectType }
    interface IEvent
