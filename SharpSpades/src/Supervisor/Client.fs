namespace SharpSpades.Supervisor

open System.Net
open SharpSpades

type ClientStats = {
    Address : IPEndPoint
    IncomingBandwidth : uint
    OutgoingBandwidth : uint
    PacketLoss : uint
    RoundTripTime : uint
}

type IClient =
    abstract member Id : ClientId
