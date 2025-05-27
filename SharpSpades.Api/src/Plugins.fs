// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades.Plugins

open System
open SharpSpades

[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
type PluginMainAttribute() =
    inherit Attribute()

type IServiceCollection = Microsoft.Extensions.DependencyInjection.IServiceCollection

type IRegisterEvent =
    abstract member Register<'T when 'T :> IEvent>
        : unit -> Result<unit, string>

type IRegisterEventHandler =
    abstract member Register
        : Priority -> EventHandler<'T> -> Result<unit, string>

type PluginBuilder<'T> = {
    Context : 'T
    Events : (IRegisterEvent -> Result<unit, string>) list
    EventHandlers : (IRegisterEventHandler -> Result<unit, string>) list
    Disposables : IDisposable list
    AsyncDisposables : IAsyncDisposable list
    Deinit : (unit -> Async<unit>) option
}

type WorldPluginBuilder() = class end

type SupervisorPluginBuilder() = class end

module Plugin =
    let newWorld () =
        {
            Context = WorldPluginBuilder()
            Events = []
            EventHandlers = []
            Disposables = []
            AsyncDisposables = []
            Deinit = None
        }

    let newSupervisor () =
        {
            Context = SupervisorPluginBuilder()
            Events = []
            EventHandlers = []
            Disposables = []
            AsyncDisposables = []
            Deinit = None
        }

    let listenWithPriority priority handler builder =
        let configure =
            fun (c : IRegisterEventHandler) ->
                c.Register priority handler
        { builder with EventHandlers = configure :: builder.EventHandlers }

    let listen handler builder =
        listenWithPriority Priority.Normal handler builder

    let registerEvent<'T when 'T :> IEvent> builder =
        let configure =
            fun (c : IRegisterEvent) ->
                c.Register<'T> ()
        { builder with Events = configure :: builder.Events }

    let registerDisposable disposable builder =
        { builder with Disposables = disposable :: builder.Disposables }

    let registerAsyncDisposable asyncDisposable builder =
        { builder with
            AsyncDisposables = asyncDisposable :: builder.AsyncDisposables }

    let deinit f builder =
        { builder with Deinit = Some f}
