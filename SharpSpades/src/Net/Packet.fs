// Copyright (c) JStalnac 2025
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades.Net

open System
open System.Buffers
open System.Buffers.Binary
open System.Threading
open System.Runtime.CompilerServices

type PacketType =
    | PositionData = 0
    | OrientationData = 1
    | WorldUpdate = 2
    | InputData = 3
    | WeaponInput = 4
    | Hit = 5
    | SetHP = 5
    | SpawnGrenade = 6
    | SetTool = 7
    | SetColor = 8
    | ExistingPlayer = 9
    | ShortPlayerData = 10
    | MoveObject = 11
    | CreatePlayer = 12
    | BlockAction = 13
    | BlockLine = 14
    | StateData = 15
    | KillAction = 16
    | ChatMessage = 17
    | MapStart = 18
    | MapChunk = 19
    | PlayerLeft = 20
    | TerritoryCapture = 21
    | ProgressBar = 22
    | IntelCapture = 23
    | IntelPickup = 24
    | IntelDrop = 25
    | Restock = 26
    | FogColor = 27
    | WeaponReload = 28
    | ChangeTeam = 29
    | ChangeWeapon = 30
    | VersionHandshakeInit = 31
    | VersionHandshakeResponse = 32
    | VersionRequest = 33
    | VersionResponse = 34

type PacketFlags =
    | Unrealible = 0
    | Reliable = 1
    | Unsequenced = 2
    | UnsequencedReliable = 3

type Packet(id : PacketType, size : int) =
    do
        if size < 0 then
            raise (ArgumentOutOfRangeException(nameof(size)))

    let mutable buffer = ArrayPool<byte>.Shared.Rent(size + 1)
    do
        buffer[0] <- (byte)id

    let mutable references = 1

    member val Id = id
    member val Size = size
    member val Buffer = buffer

    member _.GetBody() =
        Span(buffer, 1, size)

    member _.GetBodyReadOnly() =
        ReadOnlySpan(buffer, 1, size)

    member _.AddRef() =
        Interlocked.Increment(&references) |> ignore

    member _.RemoveRef() =
        match Interlocked.Decrement(&references) with
        | 0 ->
            ArrayPool<_>.Shared.Return(buffer)
            buffer <- null
        | _ -> ()

[<Struct; IsByRefLike>]
type internal PacketWriter =
    val mutable Body : Span<byte>
    val mutable Packet : Packet

    new (packet : Packet) =
        { Body = Span(packet.Buffer); Packet = packet }

    member x.Consume(size : int) =
        x.Body <- x.Body.Slice(size)

    member x.WriteByte(b : byte) =
        x.Body[0] <- b
        x.Consume(1)

    member x.WriteSByte(b : sbyte) =
        x.Body[0] <- (byte)b
        x.Consume(1)

    member x.WriteFloat(f : float32) =
        BinaryPrimitives.WriteSingleLittleEndian(x.Body, f)
        x.Consume(4)

    member x.WriteUInt32(i : uint32) =
        BinaryPrimitives.WriteUInt32LittleEndian(x.Body, i)
        x.Consume(4)

    member x.WriteInt32(i : int32) =
        BinaryPrimitives.WriteInt32LittleEndian(x.Body, i)
        x.Consume(4)

    member x.WriteBytes(span : ReadOnlySpan<byte>) =
        span.CopyTo(x.Body)
        x.Consume(span.Length)

    member x.GetPacket() = x.Packet

[<Struct; IsByRefLike>]
type internal PacketReader =
    val mutable Body : ReadOnlySpan<byte>

    new (packet : Packet) =
        { Body = packet.GetBodyReadOnly() }

    member x.Consume(size : int) =
        x.Body <- x.Body.Slice(size)

    member x.ReadByte() : byte =
        let b = x.Body[0]
        x.Consume(1)
        b

    member x.ReadSByte() : sbyte =
        let b = (sbyte)x.Body[0]
        x.Consume(1)
        b

    member x.ReadFloat() : float32 =
        let f = BinaryPrimitives.ReadSingleLittleEndian(x.Body)
        x.Consume(4)
        f

    member x.ReadUInt32() : uint32 =
        let i = BinaryPrimitives.ReadUInt32LittleEndian(x.Body)
        x.Consume(4)
        i

    member x.ReadInt32() : int32 =
        let i = BinaryPrimitives.ReadInt32LittleEndian(x.Body)
        x.Consume(4)
        i

    member x.ReadBytes(count : int) : ReadOnlySpan<byte> =
        let span =
            if count > 0 then
                x.Body.Slice(0, count)
            else if count = 0 then
                x.Body
            else
                raise (ArgumentOutOfRangeException(nameof(count)))
                ReadOnlySpan<_>.Empty
        x.Consume(span.Length)
        span
