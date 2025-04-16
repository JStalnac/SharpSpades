// Copyright (c) JStalnac 2025
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades.Server

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Serilog
open SharpSpades

type World(id : WorldId, scope : IServiceScope) as this =
    let services = scope.ServiceProvider

    do
        let guard = services.GetRequiredService<ScopeGuard>()
        match guard.Type with
        | None -> guard.Type <- Some (ScopeType.World this)
        | Some _ -> invalidOp "The service scope is already assigned to a supervisor or world"

    let loggerFactory = LoggerFactory.Create(fun c ->
        let logger =
            LoggerConfiguration()
                .Enrich.WithProperty("SpadesWorld", id)
                .WriteTo.Logger(Log.Logger)
                .CreateLogger()
        c.AddSerilog(logger, dispose = true) |> ignore)

    let logger = loggerFactory.CreateLogger("World")

    interface IWorld with        
        member _.Id: WorldId = id
        member _.Logger = logger
        member _.LoggerFactory = loggerFactory
        member _.ServiceProvider = services       
