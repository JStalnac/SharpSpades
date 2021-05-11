namespace SharpSpades.Net.Packets.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    internal sealed class LengthAttribute : System.Attribute
    {
        public int Length { get; }

        public LengthAttribute(int length)
        {
            Length = length;
        }
    }
}