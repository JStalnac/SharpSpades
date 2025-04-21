// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades

type Event = class end

type Priority =
    | Lowest = -2
    | Low = -1
    | Normal = 0
    | High = 1
    | Highest = 2

type EventHandler<'T when 'T :> Event> = 'T -> unit
