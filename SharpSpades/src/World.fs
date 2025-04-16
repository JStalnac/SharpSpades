// Copyright (c) JStalnac 2025
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades

open System
open Microsoft.Extensions.Logging

type WorldId = string

type IWorld =
    abstract member Id : WorldId

    abstract member ServiceProvider : IServiceProvider
    abstract member Logger : ILogger
    abstract member LoggerFactory : ILoggerFactory

module World =
    let getLogger (w : IWorld) category =
        w.LoggerFactory.CreateLogger(category)

    open SharpSpades.Net

    let getLoggerT<'T> (w : IWorld) =
        w.LoggerFactory.CreateLogger<'T>()
