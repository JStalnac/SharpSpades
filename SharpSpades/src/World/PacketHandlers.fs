namespace SharpSpades.World

open Microsoft.Extensions.Logging
open SharpSpades
open SharpSpades.Net

module PacketHandlers =
    type PacketHandlerDelegate =
        delegate of IWorld * IWorldClient * Packet -> unit

    [<Struct>]
    type private DelegateContainer = {
        Delegate : PacketHandlerDelegate
    }

    let private handleExistingPlayer (w : IWorld) (client : IWorldClient) (packet) =
        let logger = World.getLogger w "ExistingPlayer"
        logger.LogInformation("Received existing player from client {ClientId}", client.Id)
        let mutable playerId = Unchecked.defaultof<_>
        let mutable team = Unchecked.defaultof<_>
        let mutable weapon = Unchecked.defaultof<_>
        let mutable tool = Unchecked.defaultof<_>
        let mutable kills = Unchecked.defaultof<_>
        let mutable blockColor = Unchecked.defaultof<_>
        let mutable name = Unchecked.defaultof<_>
        Packets.readExistingPlayer packet &playerId &team &weapon &tool &kills &blockColor &name
        Packets.makeExistingPlayer 0uy TeamType.Spectator WeaponType.Rifle Tool.Spade 0u { B = 0uy; G = 0uy; R = 0uy } "Deuce"
        |> World.sendReliable w client
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
