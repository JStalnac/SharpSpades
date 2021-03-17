using System;

namespace SharpSpades.Api.Net.Packets
{
    public interface IPacket
    {
        /// <summary>
        /// The id of the packet.
        /// </summary>
        public byte Id { get; }
        /// <summary>
        /// The length of the packet in bytes minus the packet id.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Reads the packet from the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read the packet from.</param>
        public void Read(ReadOnlySpan<byte> buffer);
        /// <summary>
        /// Writes the packet data to the buffer.
        /// </summary>
        /// <param name="buffer">A buffer with the length specified in <see cref="Length"/>.</param>
        public void WriteTo(Span<byte> buffer);
    }
}
