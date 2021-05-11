using System;

namespace SharpSpades.Net.Packets.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    internal sealed class FieldAttribute : System.Attribute
    {
        public int Index { get; }

        public FieldAttribute(int index)
        {
            Index = index;
        }
    }
}