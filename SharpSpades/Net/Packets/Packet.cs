using System;
using System.Threading.Tasks;

namespace SharpSpades.Net.Packets
{
    public abstract class Packet
    {
        /// <summary>
        /// The id of the packet.
        /// </summary>
        public abstract byte Id { get; }
        /// <summary>
        /// The length of the packet in bytes minus the packet id.
        /// </summary>
        public abstract int Length { get; }

        /// <summary>
        /// Reads the packet from the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read the packet from.</param>
        internal abstract void Read(ReadOnlySpan<byte> buffer);
        /// <summary>
        /// Writes the packet data to the buffer.
        /// </summary>
        /// <param name="buffer">A buffer with the length specified in <see cref="Length"/>.</param>
        internal abstract void Write(Span<byte> buffer);
        internal abstract Task HandleAsync(Client client);
    }
}
