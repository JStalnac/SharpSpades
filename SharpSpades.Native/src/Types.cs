/*
 * Copyright (C) 2025  JStalnac
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

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
