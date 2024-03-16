using SharpSpades.Api.Events;
using SharpSpades.Api.Plugins;

#nullable enable

namespace SharpSpades.Events
{
    internal readonly struct EventSubscription
    {
        /// <summary>
        /// Plugin that subscribed to the event.
        /// </summary>
        /// <value></value>
        public IPlugin? Plugin { get; }
        public Func<object, ValueTask> Listener { get; }
        public Priority Priority { get; }

        public EventSubscription(Func<object, ValueTask> Listener, Priority priority, IPlugin? plugin)
        {
            this.Listener = Listener;
            Priority = priority;
            Plugin = plugin;
        }
    }
}