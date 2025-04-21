namespace SharpSpades

open System

[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
type PluginMainAttribute() =
    inherit Attribute()

type IServiceCollection = Microsoft.Extensions.DependencyInjection.IServiceCollection

type IRegisterEvent =
    abstract member Register<'T when 'T :> Event> : unit -> unit

type IRegisterEventHandler =
    abstract member Register
        : Priority -> EventHandler<'T> -> unit

type PluginBuilder = {
    Events : (IRegisterEvent -> unit) list
    EventHandlers : (IRegisterEventHandler -> unit) list
    Disposables : IDisposable list
    AsyncDisposables : IAsyncDisposable list
}

module Plugin =
    let newBuilder () =
        {
            Events = []
            EventHandlers = []
            Disposables = []
            AsyncDisposables = []
        }

    let listenWithPriority priority handler builder =
        let configure =
            fun (c : IRegisterEventHandler) ->
                c.Register priority handler
        { builder with EventHandlers = configure :: builder.EventHandlers }

    let listen priority handler =
        listenWithPriority Priority.Normal priority handler

    let registerEvent<'T when 'T :> Event> builder =
        let configure =
            fun (c : IRegisterEvent) ->
                c.Register<'T> ()
        { builder with Events = configure :: builder.Events }

    let registerDisposable disposable builder =
        { builder with Disposables = disposable :: builder.Disposables }

    let registerAsyncDisposable asyncDisposable builder =
        { builder with AsyncDisposables = asyncDisposable :: builder.AsyncDisposables }
