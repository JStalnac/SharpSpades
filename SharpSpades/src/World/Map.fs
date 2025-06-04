namespace SharpSpades.World

open System
open SharpSpades
open SharpSpades.Native

module Map =
    open System.Buffers
    type Map = {
        NativePtr : IntPtr
    }

    type MapError =
        | IOError of IO.IOError
        | OutOfMemory

    let loadMap path =
        async {
            let ptr = LibSharpSpades.map_create()
            if ptr = IntPtr.Zero then
                return Error OutOfMemory
            else
                match! IO.readFileBytes path with
                | Ok bytes ->
                    LibSharpSpades.map_load(ptr, bytes, bytes.Length)
                    return Ok { NativePtr = ptr }
                | Error err ->
                    return Error (IOError err)
        }

    let processEncodedMap f map =
        let writer = MapWriter()
        use w = fixed &writer
        if LibSharpSpades.map_writer_init(w) <> 0 then
            Error OutOfMemory
        else
            LibSharpSpades.map_write(map.NativePtr, w)
            let span = ReadOnlySpan<byte>(writer.Buffer.ToPointer(), writer.BufferLength)
            let arr = ArrayPool<byte>.Shared.Rent(writer.BufferLength)
            span.CopyTo(arr)
            let mem = ReadOnlyMemory(arr)
            try
                f (mem.Slice(0, writer.BufferLength))
            finally
                ArrayPool<byte>.Shared.Return(arr)
                LibSharpSpades.map_writer_deinit(w)
            |> Ok

