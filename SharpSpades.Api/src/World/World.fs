// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades.World

open System
open Microsoft.Extensions.Logging
open SharpSpades
open SharpSpades.Net

type WorldId = string

type IWorld =
    abstract member Id : WorldId

    abstract member ServiceProvider : IServiceProvider
    abstract member Logger : ILogger
    abstract member LoggerFactory : ILoggerFactory

    abstract member FireEvent<'T when 'T :> IEvent> : 'T -> unit
    abstract member SendReliable : IWorldClient * Packet -> unit

module World =
    let getLogger (w : IWorld) category =
        w.LoggerFactory.CreateLogger(category)

    let getLoggerT<'T> (w : IWorld) =
        w.LoggerFactory.CreateLogger<'T>()

    let fire<'T when 'T :> IEvent> (w : IWorld) (ev : 'T) =
        w.FireEvent ev

    let sendReliable (w : IWorld) (client : IWorldClient) (packet : Packet) =
        w.SendReliable(client, packet)
