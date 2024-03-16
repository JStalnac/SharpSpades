namespace SharpSpades.Api
{
    [Flags]
    public enum InputState
    {
        Up = 1 << 0,
        Down = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
        Jump = 1 << 4,
        Crouch = 1 << 5,
        Sneak = 1 << 6,
        Sprint = 1 << 7
    }
}