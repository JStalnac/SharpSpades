// Copyright (c) JStalnac 2025
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades

open System

type DisconnectReason =
    | Undefined = 0
    | Banned = 1
    | IPConnecionLimitExceeded = 2
    | WrongProtocolVersion = 3
    | ServerFull = 4
    | Kicked = 10

type Tool =
    | Spade = 0
    | Block = 1
    | Gun = 2
    | Grenade = 3

type BlockActionType =
    | Build = 0
    | GunDestroy = 1
    | SpadeDestroy  = 2
    | GrenadeDestroy = 3

type HitType =
    | Torso = 0
    | Head = 1
    | Arms = 2
    | Legs = 3
    | Melee = 4

type KillType =
    | Weapon = 0
    | Headshot = 1
    | Meleee = 2
    | Grenade = 3
    | Fall = 4
    | TeamChange = 5
    | WeaponChange = 6

type TeamType =
    | Spectator = -1
    | Blue = 0
    | Green = 1

type WeaponType =
    | Rifle = 0
    | Smg = 1
    | Shotgun = 2

type DamageType =
    | Fall = 0
    | Weapon = 1

type ChatMessageType =
    | All = 0
    | Team = 1
    | System = 2
    | Big = 3
    | Notice = 4
    | Warning = 5
    | Error = 6

type ClientBrand =
    | OpenSpades
    | BetterSpades
    | Ace
    | Unknown of char

[<Flags>]
type InputState =
    | Up = (1 <<< 0)
    | Down = (1 <<< 1)
    | Left = (1 <<< 2)
    | Right = (1 <<< 3)
    | Jump = (1 <<< 4)
    | Crouch = (1 <<< 5)
    | Sneak = (1 <<< 6)
    | Sprint = (1 <<< 7)
