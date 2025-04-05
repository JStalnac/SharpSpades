using SharpSpades.Api;
using System.Runtime.InteropServices;

namespace SharpSpades.Native;

[StructLayout(LayoutKind.Sequential)]
public struct Player
{
    public Tool Tool { get; set; }
    public byte Forward { get; set; }
    public byte Backward { get; set; }
    public byte Left { get; set; }
    public byte Right { get; set; }
    public byte Jump { get; set; }
    public byte Crouch { get; set; }
    public byte Sneak { get; set; }
    public byte Sprint { get; set; }
    public byte PrimaryFire { get; set; }
    public byte SecondaryFire { get; set; }

    private Vec3f position;
    private Vec3f eyePosition;
    private Vec3f velocity;
    private Vec3f strafeOrientation;
    private Vec3f heightOrientation;
    private Vec3f orientation; // Forward orientation
    private Vec3f previousOrientation;

    public byte Airborne { get; }
    public byte Wade { get; }
    public float LastClimb { get; }

    public Vec3f Position
    {
        get => position;
        set
        {
            if (!Single.IsFinite(value.X)
                || !Single.IsFinite(value.Y)
                || !Single.IsFinite(value.Z))
            {
                throw new ArgumentException("Vector has one or more components that are non-finite", nameof(value));
            }
            position = value;
            eyePosition = value;
        }
    }

    public Vec3f EyePosition => eyePosition;

    public Vec3f Velocity
    {
        get => velocity;
        set
        {
            if (!Single.IsFinite(value.X)
                || !Single.IsFinite(value.Y)
                || !Single.IsFinite(value.Z))
            {
                throw new ArgumentException("Vector has one or more components that are non-finite", nameof(value));
            }
            velocity = value;
        }
    }

    public unsafe Vec3f Orientation
    {
        get => orientation;
        set
        {
            if (!Single.IsFinite(value.X)
                || !Single.IsFinite(value.Y)
                || !Single.IsFinite(value.Z))
            {
                throw new ArgumentException("Vector has one or more components that are non-finite", nameof(value));
            }
            fixed (Player* self = &this)
            {
                LibSharpSpades.player_set_orientation(self, value);
            }
        }
    }

    public void SetInputs(InputState input)
    {
        Forward = (byte)(input.HasFlag(InputState.Up) ? 1 : 0);
        Backward = (byte)(input.HasFlag(InputState.Down) ? 1 : 0);
        Left = (byte)(input.HasFlag(InputState.Left) ? 1 : 0);
        Right = (byte)(input.HasFlag(InputState.Right) ? 1 : 0);
        Jump = (byte)(input.HasFlag(InputState.Jump) ? 1 : 0);
        Crouch = (byte)(input.HasFlag(InputState.Crouch) ? 1 : 0);
        Sneak = (byte)(input.HasFlag(InputState.Sneak) ? 1 : 0);
        Sprint = (byte)(input.HasFlag(InputState.Sprint) ? 1 : 0);
    }

    public unsafe void Update(IntPtr map, float delta, float time)
    {
        fixed (Player* self = &this)
        {
            LibSharpSpades.move_player(map, self, delta, time);
        }
    }
}
