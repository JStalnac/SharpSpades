// Copyright (c) JStalnac 2025
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

module SharpSpades.Net.Packets

open System
open System.Text
open SharpSpades
open Microsoft.FSharp.NativeInterop

// TODO: Put the Encoding.GetEncoding(437) stuff in a helper function

// TODO: World Update

let readWeaponInput packet (player : outref<_>) (primaryFire : outref<_>) (secondaryFire : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)
    let input = r.ReadByte()
    primaryFire <- false
    secondaryFire <- false
    if input &&& 0x01uy <> 0uy then
        primaryFire <- true
    if input &&& 0x02uy <> 0uy then
        secondaryFire <- true

let makeWeaponInput player primaryFire secondaryFire =
    let w = PacketWriter(Packet(PacketType.WeaponInput, 2))
    w.WriteByte(player : PlayerId)
    let primary = if primaryFire then 1uy else 0uy
    let secondary = if secondaryFire then 2uy else 0uy
    let input = 0x00uy ||| primary ||| secondary
    w.WriteByte(input)
    w.GetPacket()

let readSetColor packet (player : outref<_>) (color : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)
    color <-
        {
            B = r.ReadByte()
            G = r.ReadByte()
            R = r.ReadByte()
        }

let makeSetColor player (color : Color3) =
    let w = PacketWriter(Packet(PacketType.SetColor, 1 + 3))
    w.WriteByte((byte)(player : PlayerId))
    w.WriteByte(color.B)
    w.WriteByte(color.G)
    w.WriteByte(color.R)
    w.GetPacket()

let readExistingPlayer packet (player : outref<PlayerId>) (team : outref<TeamType>) (weapon : outref<WeaponType>) (tool : outref<Tool>) (kills : outref<uint32>) (blockColor : outref<Color3>) (name : outref<string>) =
    let r = PacketReader(packet)
    player <- r.ReadByte()
    team <- enum<_>((int32)(r.ReadByte()))
    weapon <- enum<_>((int32)(r.ReadByte()))
    tool <- enum<_>((int32)(r.ReadByte()))
    kills <- r.ReadUInt32()
    blockColor <-
        {
            B = r.ReadByte()
            G = r.ReadByte()
            R = r.ReadByte()
        }
    let rest = r.ReadBytes(0)
    if rest.Length > 16 then
        failwith "Name must be less than 16 bytes long"
    let encoding = Encoding.GetEncoding(437)
    for i = 0 to 16 do
        if rest[i] = 0x00uy then
            let span = rest.Slice(0, i)
            name <- encoding.GetString(span)
        else if i = 16 then
            name <- encoding.GetString(rest.Slice(0, 15))

let makeExistingPlayer (player : PlayerId) (team : TeamType) (weapon : WeaponType) (tool : Tool) (kills : uint32) (blockColor : Color3) (name : string) =
    let encoding = Encoding.GetEncoding(437)
    let name = encoding.GetBytes(name)
    if name.Length > 15 then
        failwith "Name cannot be longer than 15 bytes"
    let length = 1 + 1 + 1 + 1 + 4 + 3 + name.Length + 1
    let w = PacketWriter(Packet(PacketType.ExistingPlayer, length))
    w.WriteByte(player : PlayerId)
    w.WriteByte((byte)team)
    w.WriteByte((byte)weapon)
    w.WriteByte((byte)tool)
    w.WriteUInt32(kills)
    w.WriteByte(blockColor.B)
    w.WriteByte(blockColor.G)
    w.WriteByte(blockColor.R)
    w.WriteBytes(name)
    w.Body[w.Body.Length - 1] <- 0x00uy
    w.GetPacket()

let makeCreatePlayer (player : PlayerId) (weapon : WeaponType) (team : TeamType) (position : Vec3f) (name : string) =
    let encoding = Encoding.GetEncoding(437)
    let name = encoding.GetBytes(name)
    if name.Length > 15 then
        failwith "Name cannot be longer than 15 bytes"
    let length = 1 + 1 + 1 + 1 + 4 + 3 + name.Length + 1
    let w = PacketWriter(Packet(PacketType.CreatePlayer, length))
    w.WriteByte(player)
    w.WriteByte((byte)weapon)
    w.WriteByte((byte)team)
    w.WriteFloat(position.X)
    w.WriteFloat(position.Y)
    w.WriteFloat(position.Z)
    w.WriteBytes(name)
    w.WriteByte(0uy)
    w.GetPacket()

// TODO: StateData (CTF State + TC State)

let readChatMessage packet (player : outref<_>) (messageType : outref<_>) (message : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)
    messageType <- enum<ChatMessageType>((int32)(r.ReadByte()))
    let s =
        match r.ReadBytes(0) with
        | s when s.IsEmpty
            -> s
        | s when s[s.Length - 1 ] = 0uy
            -> s.Slice(0, s.Length - 1)
        | s -> s
    message <-
        match s[0] with
        | 0xFFuy -> Encoding.UTF8.GetString(s.Slice(1))
        | _ ->
            let encoding = Encoding.GetEncoding(437)
            encoding.GetString(s)

let makeChatMessage sender messageType message useUtf8 =
    let b = 
        if useUtf8 then
            Encoding.UTF8.GetBytes(message : string)
        else
            let encoding = Encoding.GetEncoding(437)
            encoding.GetBytes(message)

    let w = PacketWriter(Packet(PacketType.ChatMessage, 1 + 1 + b.Length + 1))
    w.WriteByte((byte)(sender : PlayerId))
    w.WriteByte((byte)(messageType : ChatMessageType))
    w.WriteBytes(b)
    w.WriteByte(0uy)
    w.GetPacket()

let makeFogColor (color : Color4) =
    let w = PacketWriter(Packet(PacketType.FogColor, 4))
    w.WriteByte(color.A)
    w.WriteByte(color.B)
    w.WriteByte(color.G)
    w.WriteByte(color.R)
    w.GetPacket()

let makeMapChunk (buffer : ReadOnlySpan<byte>) =
    let w = PacketWriter(Packet(PacketType.MapChunk, buffer.Length))
    w.WriteBytes(buffer)
    w.GetPacket()

let makeVersionRequest () =
    Packet(PacketType.VersionRequest, 0)

let readVersionResponse packet (clientBrand : outref<ClientBrand>) (version : outref<byte * byte * byte>) (extraInfo : outref<string>) =
    let r = PacketReader(packet)
    let encoding = Encoding.GetEncoding(437)
    clientBrand <-
        let span =
            let p = NativePtr.stackalloc<byte> 1 |> NativePtr.toVoidPtr
            Span(p, 1)
        span[0] <- r.ReadByte()
        match encoding.GetString(span)[0] with
        | 'o' -> OpenSpades
        | 'B' -> BetterSpades
        | 'a' -> Ace
        | c -> Unknown c
    version <- (r.ReadByte(), r.ReadByte(), r.ReadByte())
    let rest = r.ReadBytes(0)
    extraInfo <-
        if rest.Length = 0 then
            String.Empty
        else
            if rest[rest.Length - 1] = 0x00uy then
                encoding.GetString(rest.Slice(0, rest.Length - 1))
            else
                encoding.GetString(rest)



// The following code is generated by GenPackets.fsx
// If you want to edit it, do it through the script.

let readPositionData packet (position : outref<_>) =
    let r = PacketReader(packet)
    position <- ({ X = r.ReadFloat(); Y = r.ReadFloat(); Z = r.ReadFloat() } : Vec3f)

let makePositionData position =
    let w = PacketWriter(Packet(PacketType.PositionData, 12))
    let vec : Vec3f = position
    w.WriteFloat(vec.X)
    w.WriteFloat(vec.Y)
    w.WriteFloat(vec.Z)
    w.GetPacket()

let readOrientationData packet (orientation : outref<_>) =
    let r = PacketReader(packet)
    orientation <- ({ X = r.ReadFloat(); Y = r.ReadFloat(); Z = r.ReadFloat() } : Vec3f)

let makeOrientationData orientation =
    let w = PacketWriter(Packet(PacketType.OrientationData, 12))
    let vec : Vec3f = orientation
    w.WriteFloat(vec.X)
    w.WriteFloat(vec.Y)
    w.WriteFloat(vec.Z)
    w.GetPacket()

let readInputData packet (player : outref<_>) (input : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)
    input <- enum<InputState>((int32)(r.ReadByte()))

let makeInputData player input =
    let w = PacketWriter(Packet(PacketType.InputData, 2))
    w.WriteByte((byte)(player : PlayerId))
    w.WriteByte((byte)(input : InputState))
    w.GetPacket()

let readHit packet (playerHit : outref<_>) (hitType : outref<_>) =
    let r = PacketReader(packet)
    playerHit <- (r.ReadByte() : PlayerId)
    hitType <- enum<HitType>((int32)(r.ReadByte()))

let makeHit playerHit hitType =
    let w = PacketWriter(Packet(PacketType.Hit, 2))
    w.WriteByte((byte)(playerHit : PlayerId))
    w.WriteByte((byte)(hitType : HitType))
    w.GetPacket()

let readSetHP packet (health : outref<_>) (damageType : outref<_>) (source : outref<_>) =
    let r = PacketReader(packet)
    health <- r.ReadByte()
    damageType <- enum<DamageType>((int32)(r.ReadByte()))
    source <- ({ X = r.ReadFloat(); Y = r.ReadFloat(); Z = r.ReadFloat() } : Vec3f)

let makeSetHP health damageType source =
    let w = PacketWriter(Packet(PacketType.SetHP, 14))
    w.WriteByte(health)
    w.WriteByte((byte)(damageType : DamageType))
    let vec : Vec3f = source
    w.WriteFloat(vec.X)
    w.WriteFloat(vec.Y)
    w.WriteFloat(vec.Z)
    w.GetPacket()

let readSpawnGrenade packet (player : outref<_>) (fuse : outref<_>) (position : outref<_>) (velocity : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)
    fuse <- r.ReadFloat()
    position <- ({ X = r.ReadFloat(); Y = r.ReadFloat(); Z = r.ReadFloat() } : Vec3f)
    velocity <- ({ X = r.ReadFloat(); Y = r.ReadFloat(); Z = r.ReadFloat() } : Vec3f)

let makeSpawnGrenade player fuse position velocity =
    let w = PacketWriter(Packet(PacketType.SpawnGrenade, 29))
    w.WriteByte((byte)(player : PlayerId))
    w.WriteFloat(fuse)
    let vec : Vec3f = position
    w.WriteFloat(vec.X)
    w.WriteFloat(vec.Y)
    w.WriteFloat(vec.Z)
    let vec : Vec3f = velocity
    w.WriteFloat(vec.X)
    w.WriteFloat(vec.Y)
    w.WriteFloat(vec.Z)
    w.GetPacket()

let readSetTool packet (player : outref<_>) (tool : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)
    tool <- enum<Tool>((int32)(r.ReadByte()))

let makeSetTool player tool =
    let w = PacketWriter(Packet(PacketType.SetTool, 2))
    w.WriteByte((byte)(player : PlayerId))
    w.WriteByte((byte)(tool : Tool))
    w.GetPacket()

let readBlockAction packet (player : outref<_>) (action : outref<_>) (location : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)
    action <- enum<BlockActionType>((int32)(r.ReadByte()))
    location <- ({ X = r.ReadInt32(); Y = r.ReadInt32(); Z = r.ReadInt32() } : Location)

let makeBlockAction player action location =
    let w = PacketWriter(Packet(PacketType.BlockAction, 14))
    w.WriteByte((byte)(player : PlayerId))
    w.WriteByte((byte)(action : BlockActionType))
    let loc : Location = location
    w.WriteInt32(loc.X)
    w.WriteInt32(loc.Y)
    w.WriteInt32(loc.Z)
    w.GetPacket()

let readBlockLine packet (player : outref<_>) (startPos : outref<_>) (endPos : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)
    startPos <- ({ X = r.ReadInt32(); Y = r.ReadInt32(); Z = r.ReadInt32() } : Location)
    endPos <- ({ X = r.ReadInt32(); Y = r.ReadInt32(); Z = r.ReadInt32() } : Location)

let makeBlockLine player startPos endPos =
    let w = PacketWriter(Packet(PacketType.BlockLine, 25))
    w.WriteByte((byte)(player : PlayerId))
    let loc : Location = startPos
    w.WriteInt32(loc.X)
    w.WriteInt32(loc.Y)
    w.WriteInt32(loc.Z)
    let loc : Location = endPos
    w.WriteInt32(loc.X)
    w.WriteInt32(loc.Y)
    w.WriteInt32(loc.Z)
    w.GetPacket()

let readKillAction packet (player : outref<_>) (killer : outref<_>) (killType : outref<_>) (respawnTime : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)
    killer <- (r.ReadByte() : PlayerId)
    killType <- enum<KillType>((int32)(r.ReadByte()))
    respawnTime <- r.ReadByte()

let makeKillAction player killer killType respawnTime =
    let w = PacketWriter(Packet(PacketType.KillAction, 4))
    w.WriteByte((byte)(player : PlayerId))
    w.WriteByte((byte)(killer : PlayerId))
    w.WriteByte((byte)killType)
    w.WriteByte(respawnTime)
    w.GetPacket()

let readMapStart packet (mapSize : outref<_>) =
    let r = PacketReader(packet)
    mapSize <- r.ReadUInt32()

let makeMapStart mapSize =
    let w = PacketWriter(Packet(PacketType.MapStart, 4))
    w.WriteUInt32(mapSize)
    w.GetPacket()

let readPlayerLeft packet (player : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)

let makePlayerLeft player =
    let w = PacketWriter(Packet(PacketType.PlayerLeft, 1))
    w.WriteByte((byte)(player : PlayerId))
    w.GetPacket()

let readTerritoryCapture packet (player : outref<_>) (territory : outref<_>) (winning : outref<_>) (team : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)
    territory <- r.ReadByte()
    winning <- r.ReadByte()
    team <- r.ReadByte()

let makeTerritoryCapture player territory winning team =
    let w = PacketWriter(Packet(PacketType.TerritoryCapture, 4))
    w.WriteByte((byte)(player : PlayerId))
    w.WriteByte(territory)
    w.WriteByte(winning)
    w.WriteByte(team)
    w.GetPacket()

let readProgressBar packet (territory : outref<_>) (capturingTeam : outref<_>) (rate : outref<_>) (progress : outref<_>) =
    let r = PacketReader(packet)
    territory <- r.ReadByte()
    capturingTeam <- r.ReadByte()
    rate <- r.ReadSByte()
    progress <- r.ReadFloat()

let makeProgressBar territory capturingTeam rate progress =
    let w = PacketWriter(Packet(PacketType.ProgressBar, 7))
    w.WriteByte(territory)
    w.WriteByte(capturingTeam)
    w.WriteByte((byte)(rate : sbyte))
    w.WriteFloat(progress)
    w.GetPacket()

let readIntelCapture packet (player : outref<_>) (winning : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)
    winning <- r.ReadByte()

let makeIntelCapture player winning =
    let w = PacketWriter(Packet(PacketType.IntelCapture, 2))
    w.WriteByte((byte)(player : PlayerId))
    w.WriteByte(winning)
    w.GetPacket()

let readIntelPickup packet (player : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)

let makeIntelPickup player =
    let w = PacketWriter(Packet(PacketType.IntelPickup, 1))
    w.WriteByte((byte)(player : PlayerId))
    w.GetPacket()

let readIntelDrop packet (player : outref<_>) (position : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)
    position <- ({ X = r.ReadFloat(); Y = r.ReadFloat(); Z = r.ReadFloat() } : Vec3f)

let makeIntelDrop player position =
    let w = PacketWriter(Packet(PacketType.IntelDrop, 13))
    w.WriteByte((byte)(player : PlayerId))
    let vec : Vec3f = position
    w.WriteFloat(vec.X)
    w.WriteFloat(vec.Y)
    w.WriteFloat(vec.Z)
    w.GetPacket()

let readRestock packet (player : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)

let makeRestock player =
    let w = PacketWriter(Packet(PacketType.Restock, 1))
    w.WriteByte((byte)(player : PlayerId))
    w.GetPacket()

let readWeaponReload packet (player : outref<_>) (clipAmmo : outref<_>) (reserveAmmo : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)
    clipAmmo <- r.ReadByte()
    reserveAmmo <- r.ReadByte()

let makeWeaponReload player clipAmmo reserveAmmo =
    let w = PacketWriter(Packet(PacketType.WeaponReload, 3))
    w.WriteByte((byte)(player : PlayerId))
    w.WriteByte(clipAmmo)
    w.WriteByte(reserveAmmo)
    w.GetPacket()

let readChangeTeam packet (player : outref<_>) (team : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)
    team <- enum<TeamType>((int32)(r.ReadByte()))

let makeChangeTeam player team =
    let w = PacketWriter(Packet(PacketType.ChangeTeam, 2))
    w.WriteByte((byte)(player : PlayerId))
    w.WriteByte((byte)(team : TeamType))
    w.GetPacket()

let readChangeWeapon packet (player : outref<_>) (weapon : outref<_>) =
    let r = PacketReader(packet)
    player <- (r.ReadByte() : PlayerId)
    weapon <- enum<WeaponType>((int32)(r.ReadByte()))

let makeChangeWeapon player weapon =
    let w = PacketWriter(Packet(PacketType.ChangeWeapon, 2))
    w.WriteByte((byte)(player : PlayerId))
    w.WriteByte((byte)(weapon : WeaponType))
    w.GetPacket()

let readVersionHandshakeInit packet (challenge : outref<_>) =
    let r = PacketReader(packet)
    challenge <- r.ReadUInt32()

let makeVersionHandshakeInit challenge =
    let w = PacketWriter(Packet(PacketType.VersionHandshakeInit, 4))
    w.WriteUInt32(challenge)
    w.GetPacket()

let readVersionHandshakeResponse packet (challenge : outref<_>) =
    let r = PacketReader(packet)
    challenge <- r.ReadUInt32()

let makeVersionHandshakeResponse challenge =
    let w = PacketWriter(Packet(PacketType.VersionHandshakeResponse, 4))
    w.WriteUInt32(challenge)
    w.GetPacket()
