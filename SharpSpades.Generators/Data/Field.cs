using System;
using System.Text;

namespace SharpSpades.Generators.Data
{
    public struct Field
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string ActualType { get; set; }

        public Field()
        {
            Name = "";
            Type = "";
            ActualType = "";
        }
        
        public string GenerateProperty()
        {
            return $"public {ToClrType(ActualType)} {Name} {{ get; set; }}";
        }

        public string GenerateRead(ref int offset)
        {
            var sb = new StringBuilder();
            sb.Append($"{Name} = ");
            sb.Append(ActualType != Type ? $"({ActualType})" : "");
            
            switch (Type)
            {
                case "Byte":
                    sb.Append($"(sbyte)buffer[{offset++}];");
                    break;
                case "UByte":
                    sb.Append($"buffer[{offset++}];");
                    break;
                case "LE Float":
                    sb.Append($"buffer.ReadFloatLittleEndian({offset});");
                    offset += 4;
                    break;
                case "LE UInt":
                    sb.Append($"buffer.ReadUInt32LittleEndian({offset});");
                    offset += 4;
                    break;
                case "Vector3f":
                    sb.Append($"buffer.ReadPosition({offset});");
                    offset += 12;
                    break;
                case "Color":
                    sb.Append($"buffer.ReadColor({offset});");
                    offset += 3;
                    break;
                case "String":
                    sb.Append($"StringUtils.ReadCP437String(buffer.Slice({offset}));");
                    // Length can't be determined
                    offset = Int32.MaxValue;
                    break;
                default:
                    break;
            }
            return sb.ToString();
        }

        public string GenerateWrite(ref int offset)
        {
            string value = ActualType != Type ? $"({ToClrType(Type)}){Name}" : Name;
            string line = "";
            switch (Type)
            {
                case "Byte":
                    line = $"buffer[{offset++}] = (byte){value};";
                    break;
                case "UByte":
                    line = $"buffer[{offset++}] = {value};";
                    break;
                case "LE Float":
                    line = $"buffer.WriteFloatLittleEndian({value}, {offset});";
                    offset += 4;
                    break;
                case "LE UInt":
                    line = $"buffer.WriteUInt32LittleEndian({value}, {offset});";
                    offset += 4;
                    break;
                case "Vector3f":
                    line = $"buffer.WritePosition({value}, {offset});";
                    offset += 12;
                    break;
                case "Color":
                    line = $"buffer.WriteColor({value}, {offset});";
                    offset += 3;
                    break;
                case "String":
                    line = $"StringUtils.ToCP437String({value}).AsSpan().CopyTo(buffer.Slice({offset}));";
                    // Length can't be determined
                    offset = Int32.MaxValue;
                    break;
                default:
                    break;
            }
            return line;
        }

        private string ToClrType(string type)
            => type switch
            {
                "Byte" => "sbyte",
                "UByte" => "byte",
                "LE Float" => "float",
                "LE UInt" => "uint",
                "Vector3f" => "System.Numerics.Vector3",
                "Color" => "System.Drawing.Color",
                "String" => "string",
                _ => ActualType
            };
    }
}
