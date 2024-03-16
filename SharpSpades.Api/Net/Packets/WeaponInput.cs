namespace SharpSpades.Api.Net.Packets
{
    public partial struct WeaponInput : IPacket
    {
        public byte Id => 4;
        public int Length => 2;
        public byte PlayerId { get; set; }
        
        private byte InputState { get; set; }
        
        public bool PrimaryFire
        {
            get
            {
                return (InputState & (1 << 0)) != 0;
            }
            set
            {
                InputState = value
                    ? (byte)(InputState | (1 << 0))
                    : (byte)(InputState | (0 << 0));
            }
        }
        public bool SecondaryFire
        {
            get
            {
                return (InputState & (1 << 1)) != 0;
            }
            set
            {
                InputState = value
                    ? (byte)(InputState | (1 << 1))
                    : (byte)(InputState | (0 << 1));
            }
        }

        public void Read(ReadOnlySpan<byte> buffer)
        {
            PlayerId = buffer[0];
            InputState = buffer[1];
        }

        public void Write(Span<byte> buffer)
        {
            buffer[0] = PlayerId;
            buffer[1] = InputState;
        }
    }
}