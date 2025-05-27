// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades

open SharpSpades.Supervisor
open SharpSpades.World

[<RequireQualifiedAccess>]
type ScopeType = Supervisor of ISupervisor | World of IWorld
type ScopeGuard = { mutable Type : ScopeType option }
