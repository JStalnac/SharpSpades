/*
 * Copyright (C) 2025 JStalnac
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

public static partial class LibSharpSpades
{
    public const string LibraryName = "sharpspades";

    [LibraryImport(LibraryName, EntryPoint = nameof(map_create))]
    public static partial IntPtr map_create();

    [LibraryImport(LibraryName, EntryPoint = nameof(map_destroy))]
    public static partial void map_destroy(IntPtr map);

    [LibraryImport(LibraryName, EntryPoint = nameof(map_load))]
    public static partial void map_load(IntPtr map, ReadOnlySpan<byte> v, int len);

    [LibraryImport(LibraryName, EntryPoint = nameof(map_set))]
    public static partial void map_set(IntPtr map, ushort x, ushort y, ushort z, Block b);

    [LibraryImport(LibraryName, EntryPoint = nameof(map_get))]
    public static partial Block map_get(IntPtr map, ushort x, ushort y, ushort z);

    [LibraryImport(LibraryName, EntryPoint = nameof(map_is_solid))]
    public static partial int map_is_solid(IntPtr map, ushort x, ushort y, ushort z);

    [LibraryImport(LibraryName, EntryPoint = nameof(map_is_surface))]
    public static partial int map_is_surface(IntPtr map, ushort x, ushort y, ushort z);

    [LibraryImport(LibraryName, EntryPoint = nameof(player_create))]
    public static unsafe partial Player* player_create();

    [LibraryImport(LibraryName, EntryPoint = nameof(player_destroy))]
    public static unsafe partial void player_destroy(Player* player);

    [LibraryImport(LibraryName, EntryPoint = nameof(player_set_orientation))]
    public static unsafe partial void player_set_orientation(Player* player, Vec3f orientation);

    [LibraryImport(LibraryName, EntryPoint = nameof(player_try_uncrouch))]
    public static unsafe partial int player_try_uncrouch(IntPtr map, Player* player);

    [LibraryImport(LibraryName, EntryPoint = nameof(move_player))]
    public static unsafe partial long move_player(IntPtr map, Player* player, float delta, float time);

    [LibraryImport(LibraryName, EntryPoint = nameof(grenade_create))]
    public static partial IntPtr grenade_create(Vec3f position, Vec3f velocity);

    [LibraryImport(LibraryName, EntryPoint = nameof(grenade_destroy))]
    public static partial void grenade_destroy(IntPtr grenade);

    [LibraryImport(LibraryName, EntryPoint = nameof(move_grenade))]
    public static partial void move_grenade(IntPtr map, IntPtr grenade, float delta);
}
