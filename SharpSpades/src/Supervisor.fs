// Copyright (c) JStalnac 2025
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades

open System
open Microsoft.Extensions.Logging

type IServiceCollection = Microsoft.Extensions.DependencyInjection.IServiceCollection

type ISupervisor =
    abstract member ServiceProvider : IServiceProvider
    abstract member Logger : ILogger
    abstract member LoggerFactory : ILoggerFactory

module Supervisor =
    let getLogger (ins : ISupervisor) category =
        ins.LoggerFactory.CreateLogger(category)

    let getLoggerT<'T> (ins : ISupervisor) =
        ins.LoggerFactory.CreateLogger<'T>()
