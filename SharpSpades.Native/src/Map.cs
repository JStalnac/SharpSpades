using System.Runtime.InteropServices;

namespace SharpSpades.Native;

[StructLayout(LayoutKind.Sequential)]
public struct MapWriter
{
    public IntPtr Buffer { get; }
    public int BufferCapacity { get; }
    public int BufferLength { get; }
}
