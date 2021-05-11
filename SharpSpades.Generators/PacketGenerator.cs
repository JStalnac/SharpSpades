using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSpades.Generators.Data;

#nullable enable

namespace SharpSpades.Generators
{
    [Generator]
    internal class PacketGenerator : ISourceGenerator
    {
        public const string BasePacket = "Packet";
        public const string ReadMethod = "Read";
        public const string WriteMethod = "Write";
        public const string FieldAttribute = "Field";
        public const string ReadOnly = "ReadOnlyAttribute";
        public const string WriteOnly = "WriteOnlyAttribute";

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver sr)
                return;

            var fields = new List<Field>();
            foreach (var field in sr.Fields)
            {
                IPropertySymbol symbol = context.Compilation.GetSemanticModel(field.SyntaxTree)
                    .GetDeclaredSymbol(field)!;
                fields.Add(new Field(field, symbol));
            }

            foreach (var group in fields.GroupBy(f => f.Symbol.ContainingType))
            {
                var cls = group.Key;
                var packetFields = group.ToList();

                string source = GenerateMethods(cls, packetFields);

                if (String.IsNullOrEmpty(source))
                    continue;

                context.AddSource($"{cls.Name}_Generated.cs", source);
            }
        }

        private static string GenerateMethods(INamedTypeSymbol cls, List<Field> fields)
        {
            fields.Sort((a, b) => a.Index.CompareTo(b.Index));

            var sourceBuilder = new StringBuilder();
            sourceBuilder.Append($@"using System;
using SharpSpades.Utils;

namespace {cls.ContainingNamespace.ToDisplayString()}
{{
    public partial class {cls.Name}
    {{
");

            bool hasRead = HasReadMethod(cls);
            if (!hasRead)
                sourceBuilder.AppendLine(GenerateRead(cls, fields));

            sourceBuilder.AppendLine();

            bool hasWrite = HasWriteMethod(cls);
            if (!hasWrite)
                sourceBuilder.AppendLine(GenerateWrite(cls, fields));

            sourceBuilder.AppendLine("    }\n}");

            if (!hasRead || !hasWrite)
                return sourceBuilder.ToString();
            else
                return "";
        }

        private static string GenerateRead(INamedTypeSymbol cls, List<Field> fields)
        {
            const string indent = "    ";

            var sourceBuilder = new StringBuilder();

            sourceBuilder.AppendLine($"internal override void {ReadMethod}(ReadOnlySpan<byte> buffer)\n{{");

            if (!cls.GetAttributes().Any(x => x.AttributeClass!.Name == WriteOnly))
            {
                int bufferIndex = 0;
                foreach (var field in fields)
                {
                    if (bufferIndex == -1)
                        throw new InvalidOperationException($"{field.Name} ({cls.Name}): The previous field must define a length");

                    var fieldBuilder = new StringBuilder(indent);
                    fieldBuilder.Append($"{field.Name} = ");

                    if (field.ActualType != field.OriginalType)
                        fieldBuilder.Append($"({field.OriginalType})");

                    switch (field.ActualType)
                    {
                        case "uint":
                            fieldBuilder.Append($"buffer.ReadUInt32LittleEndian({bufferIndex})");
                            bufferIndex += 4;
                            break;
                        case "byte":
                            fieldBuilder.Append($"buffer[{bufferIndex}]");
                            bufferIndex++;
                            break;
                        case "sbyte":
                            fieldBuilder.Append($"(sbyte)buffer[{bufferIndex}]");
                            bufferIndex++;
                            break;
                        case "System.Drawing.Color":
                            fieldBuilder.Append($"buffer.ReadColor({bufferIndex})");
                            bufferIndex += 3;
                            break;
                        case "string":
                            if (field.Length == -1)
                            {
                                fieldBuilder.Append($"StringUtils.ReadCP437String(buffer.Slice({bufferIndex}))");
                                bufferIndex = -1;
                            }
                            else
                            {
                                fieldBuilder.Append($@"StringUtils.ReadCP437String(buffer.Slice({bufferIndex}, {field.Length}))");
                                bufferIndex += field.Length;
                            }
                            break;
                        case "float":
                            fieldBuilder.Append($"buffer.ReadFloatLittleEndian({bufferIndex})");
                            bufferIndex += 4;
                            break;
                        case "System.Numerics.Vector3":
                            fieldBuilder.Append($"buffer.ReadPosition({bufferIndex})");
                            bufferIndex += 12;
                            break;
                        default:
                            throw new ArgumentException($"Unknown field type '{field.ActualType}' ({cls.Name})");
                    }

                    fieldBuilder.Append(";");
                    sourceBuilder.AppendLine(fieldBuilder.ToString());
                }

            // Add packet length assertion
            if (bufferIndex != -1)
                sourceBuilder.Append($@"
{indent}if (buffer.Length > {bufferIndex})
{indent}{indent}throw new ArgumentException(""Packet is too long"");
");
            }
            else
                sourceBuilder.AppendLine($@"{indent}throw new InvalidOperationException(""The packet is write only"");");

            sourceBuilder.Append("}");

            return String.Join("\n", sourceBuilder.ToString().Split('\n')
                .Select(line => $"{indent}{indent}{line}"));
        }

        private static string GenerateWrite(INamedTypeSymbol cls, List<Field> fields)
        {
            const string indent = "    ";

            var sourceBuilder = new StringBuilder();

            sourceBuilder.AppendLine($"internal override void {WriteMethod}(Span<byte> buffer)\n{{");

            if (!cls.GetAttributes().Any(a => a.AttributeClass!.Name == ReadOnly))
            {
                int bufferIndex = 0;
                foreach (var field in fields)
                {
                    if (bufferIndex == -1)
                        throw new InvalidOperationException($"{field.Name} ({cls.Name}): The previous field must define a length");

                    var fieldBuilder = new StringBuilder(indent);

                    string value = "";
                    if (field.ActualType != field.OriginalType)
                        value = $"({field.ActualType}){field.Name}";
                    else
                        value = $"{field.Name}";

                    switch (field.ActualType)
                    {
                        case "uint":
                            fieldBuilder.Append($"buffer.WriteUInt32LittleEndian({value}, {bufferIndex})");
                            bufferIndex += 4;
                            break;
                        case "byte":
                            fieldBuilder.Append($"buffer[{bufferIndex}] = {value}");
                            bufferIndex++;
                            break;
                        case "sbyte":
                            fieldBuilder.Append($"buffer[{bufferIndex}] = (byte){value}");
                            bufferIndex++;
                            break;
                        case "System.Drawing.Color":
                            fieldBuilder.Append($"buffer.WriteColor({value}, {bufferIndex})");
                            bufferIndex += 3;
                            break;
                        case "string":
                            fieldBuilder.Append($"StringUtils.ToCP437String({value}).AsSpan().CopyTo(buffer.Slice({bufferIndex}");
                            if (field.Length != -1)
                            {
                                fieldBuilder.Append($", {field.Length}");
                                bufferIndex += field.Length;
                            }
                            else
                                bufferIndex = -1;
                            fieldBuilder.Append("))");
                            break;
                        case "float":
                            fieldBuilder.Append($"buffer.WriteFloatLittleEndian({value}, {bufferIndex})");
                            bufferIndex += 4;
                            break;
                        case "System.Numerics.Vector3":
                            fieldBuilder.Append($"buffer.WritePosition({value}, {bufferIndex})");
                            bufferIndex += 12;
                            break;
                        default:
                            throw new ArgumentException($"Unknown field type '{field.ActualType}' ({cls.Name})");
                    }

                    fieldBuilder.Append(";");
                    sourceBuilder.AppendLine(fieldBuilder.ToString());
                }
            }
            else
                sourceBuilder.AppendLine($@"{indent}throw new InvalidOperationException(""The packet is read only"");");

            sourceBuilder.Append("}");

            return String.Join("\n", sourceBuilder.ToString().Split('\n')
                .Select(line => $"{indent}{indent}{line}"));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<PropertyDeclarationSyntax> Fields { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode node)
            {
                if (node is ClassDeclarationSyntax cls)
                {
                    if (!cls.BaseList?.ChildNodes().Any(x => x.ToString() == BasePacket) ?? false)
                        return;

                    Fields.AddRange(cls.ChildNodes()
                        .Where(x => x is PropertyDeclarationSyntax)
                        .Cast<PropertyDeclarationSyntax>()
                        .Where(m => m.HasAttribute(FieldAttribute)));
                }
            }
        }

        private static bool HasReadMethod(INamedTypeSymbol symbol)
            => symbol.GetMembers().Where(m => m is IMethodSymbol)
                .Any(m => m.Name == ReadMethod && m.IsOverride);

        private static bool HasWriteMethod(INamedTypeSymbol symbol)
            => symbol.GetMembers().Where(m => m is IMethodSymbol)
                .Any(m => m.Name == WriteMethod && m.IsOverride);
    }
}