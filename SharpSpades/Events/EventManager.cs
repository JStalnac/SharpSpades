using Microsoft.Extensions.Logging;
using SharpSpades.Api.Events;
using SharpSpades.Api.Plugins;
using SharpSpades.Utils;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace SharpSpades.Events;

public class EventManager : IEventManager
{
    private readonly ConcurrentDictionary<Type, EventRegistration> events = new();

    private readonly ILogger<EventManager> logger;

    public EventManager(ILogger<EventManager> logger)
    {
        this.logger = logger;
    }

    internal void Register<TEvent>() where TEvent : Event
    {
        events.TryAdd(typeof(TEvent), new EventRegistration(null));
    }

    public void Register<TEvent>(IPlugin plugin) where TEvent : Event
    {
        Throw.IfNull(plugin);
        // TODO: Check if plugin is enabled
        events.TryAdd(typeof(TEvent), new EventRegistration(plugin));
    }

    public void Subscribe<TEvent>(IPlugin plugin, Func<TEvent, Task> listener, Priority priority = Priority.Normal) where TEvent : Event
    {
        Throw.IfNull(plugin);
        // TODO: Check if plugin is enabled

        // Do not throw because then it's easier to optionally subscribe
        // to an event in for example a class
        if (!events.TryGetValue(typeof(TEvent), out var reg))
            return;

        reg.AddListener(new EventSubscription(Wrapper, priority, plugin));

        // Use a wrapper so that we can have void and ValueTask based
        // listeners in the future without having to modify the internals
        // too much
        async ValueTask Wrapper(object e)
            => await listener((TEvent)e);
    }

    internal void RemovePlugin([AllowNull]IPlugin plugin)
    {
        // Events may get added after we have enumerated past
        // the point in the collection but those should be for
        // other plugins, so having no external lock is fine
        foreach (var e in events)
        {
            if (e.Value.Plugin == plugin)
                events.TryRemove(e.Key, out var _);
        }

        foreach (var (_, r) in events)
            r.RemovePlugin(plugin);
    }

    public async Task<TEvent> FireAsync<TEvent>(TEvent ev) where TEvent : Event
    {
        Throw.IfNull(ev);
        await FireInternalAsync(ev);
        return ev;
    }

    public void FireAndForget<TEvent>(TEvent ev) where TEvent : Event
    {
        Throw.IfNull(ev);
        if (!events.TryGetValue(typeof(TEvent), out var _))
            throw new InvalidOperationException($"No event of type {typeof(TEvent)} has been registered");
        _ = FireInternalAsync(ev);
    }

    private async ValueTask FireInternalAsync<TEvent>(TEvent ev) where TEvent : Event
    {
        if (!events.TryGetValue(typeof(TEvent), out var reg))
            throw new InvalidOperationException($"No event of type {typeof(TEvent)} has been registered");

        for (int i = 0; i < reg.Listeners.Length; i++)
        {
            foreach (var s in reg.Listeners[i])
            {
                try
                {
                    await s.Listener(ev);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, $"Event handler for {typeof(TEvent)} threw an unhandled exception");
                }
            }
        }
    }
}
