using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpSpades.Generators.Data
{
    internal struct Field
    {
        private const string ActualTypeAttribute = "ActualType";
        private const string LengthAttribute = "Length";

        public int Index { get; }
        public string Name { get; }
        public string OriginalType { get; }
        public string ActualType { get; }
        public int Length { get; }
        public PropertyDeclarationSyntax Declaration { get; }
        public IPropertySymbol Symbol { get; }

        private static List<string> KnownTypes { get; } = new()
        {
            "uint",
            "byte",
            "sbyte",
            "float",
            "string",
            "System.Drawing.Color",
            "System.Numerics.Vector3"
        };

        public Field(PropertyDeclarationSyntax dec, IPropertySymbol symbol)
        {
            Declaration = dec;
            Symbol = symbol;
            Name = symbol.Name;
            OriginalType = symbol.Type.ToString();
            ActualType = symbol.Type.ToString();
            Length = -1;

            var attributes = dec.AttributeLists.SelectMany(list => list.Attributes);

            Index = Int32.Parse(attributes.First(a => a.Name.ToString() == PacketGenerator.FieldAttribute)
                        .ArgumentList.Arguments.First().GetText().ToString());
            
            foreach (var a in attributes)
            {
                var args = a.ArgumentList.Arguments;
                switch (a.Name.ToString())
                {
                    case ActualTypeAttribute:
                        var @typeof = a.DescendantNodes().First(node => node is TypeOfExpressionSyntax) as TypeOfExpressionSyntax;
                        
                        // The compiler type name must be used directly, for example
                        // having System.String as the type wouldn't work
                        ActualType = @typeof.Type.GetText().ToString();
                        break;
                    case LengthAttribute:
                        var s = a.DescendantNodes().First(node => node is LiteralExpressionSyntax) as LiteralExpressionSyntax;
                        Length = Int32.Parse(s.ToString());
                        break;
                    default:
                        break;
                }
            }

            if (!KnownTypes.Contains(ActualType))
                throw new ArgumentException($"Type '{ActualType}' is not supported for packet fields ({Symbol.ContainingType.Name})");
        }
    }
}