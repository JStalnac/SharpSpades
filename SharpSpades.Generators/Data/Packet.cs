using System.Collections.Generic;

namespace SharpSpades.Generators.Data
{
    public struct Packet
    {
        public string Name { get; set; }
        public byte Id { get; set; }
        public int Length { get; set; }
        public List<Field> Fields { get; set; }
        
        public Packet()
        {
            Name = "";
            Id = 0;
            Length = 0;
            Fields = new();
        }
    }
}
