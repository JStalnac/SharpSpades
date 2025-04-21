#!/usr/bin/env -S dotnet fsi --
// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

open System.IO

type Field =
    | Byte of string
    | SByte of string
    | Player of string
    | InputState of string
    | HitType of string
    | DamageType of string
    | Tool of string
    | TeamType of string
    | WeaponType of string
    | BlockAction of string
    | KillType of string
    | UInt32 of string
    | Location of string
    | Float of string
    | Vec3f of string

let getSize field =
    match field with
    | Byte _ | SByte _ | Player _ | InputState _ | HitType _  | DamageType _
    | Tool _ | TeamType _ | WeaponType _ | BlockAction _  | KillType _
        -> 1
    | UInt32 _ -> 4
    | Location _ -> 3 * 4
    | Float _ -> 4
    | Vec3f _ -> 3 * 4

let getName field =
    match field with
    | Byte n | SByte n | Player n | InputState n | HitType n | DamageType n
    | Tool n | TeamType n | WeaponType n | BlockAction n  | KillType n
    | UInt32 n | Location n | Float n | Vec3f n -> n

type Packet = { Name : string; Fields : Field list }

let packet name fields = { Name = name; Fields = fields }

let generate p : string=
    let length = List.sumBy getSize p.Fields

    [
        sprintf "let read%s packet %s ="
            p.Name
            (p.Fields
            |> List.map (fun f -> sprintf "(%s : outref<_>)" (getName f))
            |> String.concat " ")

        "    let r = PacketReader(packet)"

        for f in p.Fields do
            match f with
            | Byte n
                -> sprintf "    %s <- r.ReadByte()" n
            | SByte n
                -> sprintf "    %s <- r.ReadSByte()" n
            | Player n
                -> sprintf "    %s <- (r.ReadByte() : PlayerId)" n
            | InputState n
                -> sprintf "    %s <- enum<InputState>((int32)(r.ReadByte()))" n
            | HitType n
                -> sprintf "    %s <- enum<HitType>((int32)(r.ReadByte()))" n
            | DamageType n
                -> sprintf "    %s <- enum<DamageType>((int32)(r.ReadByte()))" n
            | Tool n
                -> sprintf "    %s <- enum<Tool>((int32)(r.ReadByte()))" n
            | TeamType n
                -> sprintf "    %s <- enum<TeamType>((int32)(r.ReadByte()))" n
            | WeaponType n
                -> sprintf "    %s <- enum<WeaponType>((int32)(r.ReadByte()))" n
            | BlockAction n
                -> sprintf "    %s <- enum<BlockActionType>((int32)(r.ReadByte()))" n
            | KillType n
                -> sprintf "    %s <- enum<KillType>((int32)(r.ReadByte()))" n
            | UInt32 n
                -> sprintf "    %s <- r.ReadUInt32()" n
            | Location n
                -> sprintf "    %s <- ({ X = r.ReadInt32(); Y = r.ReadInt32(); Z = r.ReadInt32() } : Location)" n
            | Float n
                -> sprintf "    %s <- r.ReadFloat()" n
            | Vec3f n
                -> sprintf "    %s <- ({ X = r.ReadFloat(); Y = r.ReadFloat(); Z = r.ReadFloat() } : Vec3f)" n

        ""

        sprintf "let make%s %s ="
            p.Name
            (p.Fields |> List.map getName |> String.concat " ")

        sprintf "    let w = PacketWriter(Packet(PacketType.%s, %d))"
            p.Name length

        for f in p.Fields do
            match f with
            | Byte n
                -> sprintf "    w.WriteByte(%s)" n
            | SByte n
                -> sprintf "    w.WriteByte((byte)(%s : sbyte))" n
            | Player n
                -> sprintf "    w.WriteByte((byte)(%s : PlayerId))" n
            | InputState n
                -> sprintf "    w.WriteByte((byte)(%s : InputState))" n
            | HitType n
                -> sprintf "    w.WriteByte((byte)(%s : HitType))" n
            | DamageType n
                -> sprintf "    w.WriteByte((byte)(%s : DamageType))" n
            | Tool n
                -> sprintf "    w.WriteByte((byte)(%s : Tool))" n
            | TeamType n
                -> sprintf "    w.WriteByte((byte)(%s : TeamType))" n
            | WeaponType n
                -> sprintf "    w.WriteByte((byte)(%s : WeaponType))" n
            | BlockAction n
                -> sprintf "    w.WriteByte((byte)(%s : BlockActionType))" n
            | KillType n
                -> sprintf "    w.WriteByte((byte)%s)" n
            | UInt32 n
                -> sprintf "    w.WriteUInt32(%s)" n
            | Location n
                -> yield! [
                    sprintf "    let loc : Location = %s" n
                    "    w.WriteInt32(loc.X)"
                    "    w.WriteInt32(loc.Y)"
                    "    w.WriteInt32(loc.Z)"
                ]

            | Float n
                -> sprintf "    w.WriteFloat(%s)" n
            | Vec3f n
                -> yield! [
                    sprintf "    let vec : Vec3f = %s" n
                    "    w.WriteFloat(vec.X)"
                    "    w.WriteFloat(vec.Y)"
                    "    w.WriteFloat(vec.Z)"
                ]

        "    w.GetPacket()"
    ]
    |> String.concat "\n"

[
    packet "PositionData" [
        Vec3f "position"
    ]
    packet "OrientationData" [
        Vec3f "orientation"
    ]
    packet "InputData" [
        Player "player"
        InputState "input"
    ]
    packet "Hit" [
        Player "playerHit"
        HitType "hitType"
    ]
    packet "SetHP" [
        Byte "health"
        DamageType "damageType"
        Vec3f "source"
    ]
    packet "SpawnGrenade" [
        Player "player"
        Float "fuse"
        Vec3f "position"
        Vec3f "velocity"
    ]
    packet "SetTool" [
        Player "player"
        Tool "tool"
    ]
    packet "BlockAction" [
        Player "player"
        BlockAction "action"
        Location "location"
    ]
    packet "BlockLine" [
        Player "player"
        Location "startPos"
        Location "endPos"
    ]
    packet "KillAction" [
        Player "player"
        Player "killer"
        KillType "killType"
        Byte "respawnTime"
    ]
    packet "MapStart" [
        UInt32 "mapSize"
    ]
    packet "PlayerLeft" [
        Player "player"
    ]
    packet "TerritoryCapture" [
        Player "player"
        Byte "territory"
        Byte "winning"
        Byte "team"
    ]
    packet "ProgressBar" [
        Byte "territory"
        Byte "capturingTeam"
        SByte "rate"
        Float "progress"
    ]
    packet "IntelCapture" [
        Player "player"
        Byte "winning"
    ]
    packet "IntelPickup" [
        Player "player"
    ]
    packet "IntelDrop" [
        Player "player"
        Vec3f "position"
    ]
    packet "Restock" [
        Player "player"
    ]
    packet "WeaponReload" [
        Player "player"
        Byte "clipAmmo"
        Byte "reserveAmmo"
    ]
    packet "ChangeTeam" [
        Player "player"
        TeamType "team"
    ]
    packet "ChangeWeapon" [
        Player "player"
        WeaponType "weapon"
    ]
    packet "VersionHandshakeInit" [
        UInt32 "challenge"
    ]
    packet "VersionHandshakeResponse" [
        UInt32 "challenge"
    ]
]
|> List.map generate
|> String.concat "\n\n"
|> fun generated ->
    [
        "// The following code is generated by GenPackets.fsx"
        "// If you want to edit it, do it through the script."
        ""
        generated
    ]
    |> String.concat "\n"
|> fun contents ->
    File.WriteAllText("GeneratedPackets.fs", contents)
