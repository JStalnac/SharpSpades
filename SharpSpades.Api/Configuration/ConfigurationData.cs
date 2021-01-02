using System;
using Tommy;

namespace SharpSpades.Api.Configuration
{
    internal record Table
    {
        public string Name { get; init; }
        public bool Required { get; init; }
        public string Comment { get; init; }
        public Table[] Tables { get; init; }
        public Field[] Fields { get; init; }
    }

    internal record Field()
    {
        public string Name { get; init; }
        public Type Type { get; init; }
        public TomlNode InitialValue { get; init; }
        public bool Required { get; init; }
        public string Comment { get; init; }
        public Func<TomlNode, bool> Validator { get; init; }
    }
}
