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

namespace SharpSpades.Native.Net;

public enum AddressType
{
    Any,
    IPv4,
    IPv6
}

public enum CallbackResult
{
    Continue = 0,
    Stop = 1
}

public enum DisconnectType
{
    Normal = 0,
    Timeout = 1
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate CallbackResult ConnectCallback(uint client, ProtocolVersion version);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate CallbackResult ReceiveCallback(uint client, byte* buffer, int length);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate CallbackResult DisconnectCallback(uint client, DisconnectType type);

public partial class NetHost : IDisposable
{
    private readonly IntPtr host;
    private bool disposed = false;

    private NetHost(IntPtr host)
    {
        this.host = host;
    }

    public static NetHost CreateListener(
        AddressType type,
        ushort port,
        uint clients,
        uint channels,
        uint incomingBandwidth = 0,
        uint outgoingBandwidth = 0)
    {
        IntPtr host = net_host_create_listener(type, port, clients, channels, incomingBandwidth, outgoingBandwidth);
        if (host == IntPtr.Zero)
            throw new Exception("Failed to create ENet host");
        return new(host);
    }

    public void OnConnect(Func<uint, ProtocolVersion, CallbackResult> callback)
    {
        net_host_set_connect_callback(host, Callback);

        CallbackResult Callback(uint client, ProtocolVersion version)
        {
            return callback(client, version);
        }
    }

    public delegate CallbackResult ReceiveFunc(uint client, ReadOnlySpan<byte> buffer);

    public unsafe void OnReceive(ReceiveFunc callback)
    {
        net_host_set_receive_callback(host, Callback);

        CallbackResult Callback(uint client, byte* buffer, int length)
        {
            var span = new ReadOnlySpan<byte>(buffer, length);
            return callback(client, span);
        }
    }

    public void OnDisconnect(Func<uint, DisconnectType, CallbackResult> callback)
    {
        net_host_set_disconnect_callback(host, Callback);

        CallbackResult Callback(uint client, DisconnectType data)
        {
            return callback(client, data);
        }
    }

    public int PollEvents(TimeSpan timeout)
    {
        return net_host_poll_events(host, (uint)timeout.Milliseconds);
    }

    public void Dispose()
    {
        if (disposed)
            return;
        net_host_destroy(host);
        disposed = true;
    }

    [LibraryImport(LibSharpSpades.LibraryName)]
    internal static partial IntPtr net_host_create_listener(
        AddressType type,
        ushort port,
        nuint clients,
        nuint channels,
        uint incomingBandwidth,
        uint outgoingBandwidth);

    [LibraryImport(LibSharpSpades.LibraryName)]
    internal static partial IntPtr net_host_destroy(IntPtr host);

    [LibraryImport(LibSharpSpades.LibraryName)]
    internal static partial IntPtr net_host_set_connect_callback(IntPtr host, ConnectCallback callback);

    [LibraryImport(LibSharpSpades.LibraryName)]
    internal static partial IntPtr net_host_set_receive_callback(IntPtr host, ReceiveCallback callback);

    [LibraryImport(LibSharpSpades.LibraryName)]
    internal static partial IntPtr net_host_set_disconnect_callback(IntPtr host, DisconnectCallback callback);

    [LibraryImport(LibSharpSpades.LibraryName)]
    internal static partial int net_host_poll_events(IntPtr host, uint serviceTimeoutMs);
}
