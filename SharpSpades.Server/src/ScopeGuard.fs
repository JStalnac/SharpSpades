// Copyright (c) JStalnac 2025
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades.Server

open SharpSpades

[<RequireQualifiedAccess>]
type ScopeType = Supervisor of ISupervisor | World of IWorld
type ScopeGuard = { mutable Type : ScopeType option }
