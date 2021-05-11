using System;

namespace SharpSpades.Net.Packets.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    internal sealed class ActualTypeAttribute : Attribute
    {
        public Type Type { get; }

        public ActualTypeAttribute(Type type)
        {
            Type = type;
        }
    }
}