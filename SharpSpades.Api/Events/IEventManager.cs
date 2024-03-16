using SharpSpades.Api.Plugins;

namespace SharpSpades.Api.Events;

/// <summary>
/// Manages event registrations and subscriptions.
/// </summary>
public interface IEventManager
{
    /// <summary>
    /// Registers a new event for listeners to subscribe to. The event is removed when the plugin is unloaded.
    /// </summary>
    /// <param name="plugin">The plugin that registers the event.</param
    /// <typeparam name="TEvent">The type of event to register</typeparam>
    void Register<TEvent>(IPlugin plugin) where TEvent : Event;

    /// <summary>
    /// Subscribes a listener to an event.
    /// </summary>
    /// <param name="listener">The listener.</param>
    /// <param name="priority">The priority of the listener. Listeners with higher priority will run before others.</param>
    /// <typeparam name="TEvent">The type of event to subscribe to. Does not support more-derived types.</typeparam>
    void Subscribe<TEvent>(IPlugin plugin, Func<TEvent, Task> listener, Priority priority = Priority.Normal) where TEvent : Event;

    /// <summary>
    /// Fires an event and asynchronously waits for it to complete.
    /// </summary>
    /// <param name="ev">The event to fire.</param>
    /// <typeparam name="TEvent">The type of the event to fire. Does not support more-derived types.</typeparam>
    /// <returns>A task with the event object after all listeners have been invoked.</returns>
    Task<TEvent> FireAsync<TEvent>(TEvent ev) where TEvent : Event;

    /// <summary>
    /// Fires an event and returns without waiting for it to complete.
    /// </summary>
    /// <param name="ev">The event to fire.</param>
    /// <typeparam name="TEvent">The type of the event to fire. Does not support more-derived types.</typeparam>
    void FireAndForget<TEvent>(TEvent ev) where TEvent : Event;
}