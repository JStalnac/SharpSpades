using SharpSpades.Api.Events;
using SharpSpades.Api.Plugins;
using System.Diagnostics.CodeAnalysis;

namespace SharpSpades.Events;

#nullable enable

internal class EventRegistration
{
    /// <summary>
    /// Plugin that registered the event.
    /// </summary>
    /// <value></value>
    public IPlugin? Plugin { get; }
    public IGrouping<Priority, EventSubscription>[] Listeners { get; set; }
        = Array.Empty<IGrouping<Priority, EventSubscription>>();
    private readonly object writeLock = new object();
    
    public EventRegistration(IPlugin? plugin)
    {
        this.Plugin = plugin;
    }
    
    public void AddListener(EventSubscription subscription)
    {
        ModifyListeners(s => s.Append(subscription));
    }
    
    public void RemovePlugin([AllowNull]IPlugin plugin)
    {
        ModifyListeners(s => s.Where(x => plugin != x.Plugin));
    }

    private void ModifyListeners(Func<IEnumerable<EventSubscription>, IEnumerable<EventSubscription>> f)
    {
        lock (writeLock)
        {
            // Prevent race condition with event executors by replacing the collection
            // Process the priorities only once
            Listeners = f(Listeners.SelectMany(g => g))
                .GroupBy(s => s.Priority)
                .OrderByDescending(g => g.Key)
                // An array should keep the order
                .ToArray();
        }
    }
}