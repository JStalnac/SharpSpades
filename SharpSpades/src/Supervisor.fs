// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades

open System
open Microsoft.Extensions.Logging

type ISupervisor =
    abstract member ServiceProvider : IServiceProvider
    abstract member Logger : ILogger
    abstract member LoggerFactory : ILoggerFactory

    abstract member FireEvent<'T when 'T :> Event> : 'T -> unit

module Supervisor =
    let getLogger (s : ISupervisor) category =
        s.LoggerFactory.CreateLogger(category)

    let getLoggerT<'T> (s : ISupervisor) =
        s.LoggerFactory.CreateLogger<'T>()

    let fire<'T when 'T :> Event> (s : ISupervisor) (ev : 'T) =
        s.FireEvent ev
