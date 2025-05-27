// Copyright (c) 2025 JStalnac
//
// SPDX-License-Identifier: GPL-3.0-or-later OR EUPL-1.2

namespace SharpSpades

open System
open System.Collections.Generic
open Microsoft.Extensions.Logging
open SharpSpades

type private IEventRegistration = interface end

type private EventRegistration<'T when 'T :> IEvent>() =
    member _.Handlers = ResizeArray<EventHandler<'T>>()

    interface IEventRegistration

type EventManager(logger : ILogger) =
    let events = Dictionary<Type, IEventRegistration>()

    member _.RegisterEvent<'T when 'T :> IEvent>() =
        let eventType = typeof<'T>
        if eventType.IsSubclassOf(typeof<IEvent>) then
            let added = events.TryAdd(eventType, EventRegistration<'T>())
            if not added then
                logger.LogWarning("Event of type {Event} was registered multiple times, ignoring later registrations", eventType.Name)
            Ok ()
        else
            Error "Event type must inherit from Event"

    member _.RegisterHandler<'T when 'T :> IEvent>(handler) =
        let eventType = typeof<'T>
        match events.TryGetValue(eventType) with
        | true, reg ->
            let reg = reg :?> EventRegistration<'T>
            reg.Handlers.Add(handler)
            Ok ()
        | false, _ ->
            Error (sprintf "No event of type %s registered" eventType.Name)

    member _.Fire(ev : 'T) =
        let eventType = typeof<'T>
        match events.TryGetValue(eventType) with
        | true, reg ->
            let reg = reg :?> EventRegistration<'T>
            for h in reg.Handlers do
                try
                    h ev
                with
                    ex ->
                        logger.LogWarning(ex,
                            "Event handler for event {Event} threw an unhandled exception",
                            typeof<'T>.Name)
        | false, _ ->
            logger.LogWarning("Tried to fire an unregistered event of type {Event}",
                        typeof<'T>.Name)
