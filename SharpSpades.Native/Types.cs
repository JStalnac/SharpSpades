using System.Runtime.InteropServices;

namespace SharpSpades.Native;

[StructLayout(LayoutKind.Sequential)]
public struct Vec3f
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

[StructLayout(LayoutKind.Explicit)]
public readonly struct Block
{
    [FieldOffset(0)]
    public readonly byte B;
    [FieldOffset(1)]
    public readonly byte G;
    [FieldOffset(2)]
    public readonly byte R;
    [FieldOffset(3)]
    public readonly byte Data;

    [FieldOffset(0)]
    public readonly uint Raw;
}

public unsafe struct Map
{
    public const int MapX = 512;
    public const int MapY = 512;
    public const int MapZ = 64;
    public const uint ColorMask = 0xFF000000;
    public const uint DefaultColor = 0xFF674028;
    public const uint Air = 0x00FFFFFF & DefaultColor;
}

[StructLayout(LayoutKind.Sequential)]
public struct Grenade
{
    public Vec3f Position { get; set; }
    public Vec3f Velocity { get; set; }
}
