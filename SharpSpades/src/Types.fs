// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades

type ClientId = uint
type PlayerId = byte

[<Struct>]
type Location = { X : int; Y : int; Z : int }

[<Struct>]
type Vec3f = { X : float32; Y : float32; Z : float32 }

type Color3 = { B : byte; G : byte; R : byte }
type Color4 = { B : byte; G : byte; R : byte; A : byte }
