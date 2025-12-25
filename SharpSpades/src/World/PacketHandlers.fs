namespace SharpSpades.World

open System
open SharpSpades.Net

module PacketHandlers =
    type PacketHandlerDelegate =
        delegate of IWorld * IWorldClient * Packet -> unit

    [<Struct>]
    type private DelegateContainer = {
        Delegate : PacketHandlerDelegate
    }

    let private handleExistingPlayer (w : IWorld) (client : IWorldClient) (packet) =
        ()

    let private handlers : DelegateContainer array =
        let handler f = { Delegate = f }
        Array.init 256 (fun i ->
            match enum i with
            | PacketType.ExistingPlayer -> handler handleExistingPlayer
            | _ -> { Delegate = null })

    let handlePacket world client (packet : Packet) =
        match handlers[int <| byte packet.Id].Delegate with
        | null -> ()
        | handler -> handler.Invoke(world, client, packet)
