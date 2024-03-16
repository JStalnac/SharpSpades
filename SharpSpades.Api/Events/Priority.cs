namespace SharpSpades.Api.Events
{
    /// <summary>
    /// Specifies the firing order of an event subscription.
    /// </summary>
    public enum Priority
    {
        Lowest = -2,
        Lower = -1,
        Normal = 0,
        Higher = 1,
        Highest = 2
    }
}
