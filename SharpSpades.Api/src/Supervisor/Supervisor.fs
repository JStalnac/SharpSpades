// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades.Supervisor

open System
open Microsoft.Extensions.Logging
open SharpSpades
open SharpSpades.Net

type ISupervisor =
    abstract member ServiceProvider : IServiceProvider
    abstract member Logger : ILogger
    abstract member LoggerFactory : ILoggerFactory

    abstract member Clients : IClient array

    abstract member FireEvent<'T when 'T :> IEvent> : 'T -> unit
    abstract member SendPacket : ClientId * PacketFlags * Packet -> unit
    abstract member GetClientStats : ClientId -> ClientStats option

module Supervisor =
    let getLogger (s : ISupervisor) category =
        s.LoggerFactory.CreateLogger(category)

    let getLoggerT<'T> (s : ISupervisor) =
        s.LoggerFactory.CreateLogger<'T>()

    let fireEvent<'T when 'T :> IEvent> (s : ISupervisor) (ev : 'T) =
        s.FireEvent ev

    let getClients (s : ISupervisor) =
        s.Clients

    let sendPacket (s : ISupervisor) client flags packet =
        s.SendPacket(client, flags, packet)

    let getClientStats (s : ISupervisor) client =
        s.GetClientStats(client)
