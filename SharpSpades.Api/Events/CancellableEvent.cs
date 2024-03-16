namespace SharpSpades.Api.Events
{
    /// <summary>
    /// An event that can be canceled.
    /// </summary>
    public abstract class CancellableEvent : Event
    {
        /// <summary>
        /// Whether the event has been cancelled.
        /// </summary>
        /// <value></value>
        public bool Cancelled { get; private set; }

        /// <summary>
        /// Cancels the event. This does not stop any event handlers from firing.
        /// </summary>
        public void Cancel()
            => Cancelled = true;
    }
}