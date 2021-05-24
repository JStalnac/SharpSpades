using System;

namespace SharpSpades.Net.Packets.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    internal sealed class FieldAttribute : Attribute
    {
        public int Index { get; }

        public FieldAttribute(int index)
        {
            Index = index;
        }
    }
}