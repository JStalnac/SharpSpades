using Microsoft.CodeAnalysis;
using SharpSpades.Generators.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpSpades.Generators
{
    [Generator]
    public class PacketGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext initContext)
        {
            // Get the protocol file
            IncrementalValuesProvider<AdditionalText> protocolFile = initContext.AdditionalTextsProvider
                            .Where(f => f.Path.EndsWith("protocol.md"));
            
            // Get the protocol file contents
            IncrementalValueProvider<string> protocolSource = protocolFile.Collect()
                    .Select(static (files, ct) => files.Single().GetText()!.ToString());
            
            // Parse packets from protocol sources
            IncrementalValueProvider<ImmutableArray<Packet>> packets = protocolSource
                    .Select(static (s, ct) => PacketGenerator.ParseProtocolSpec(s));
            
            // Generate packet classes
            IncrementalValueProvider<ImmutableArray<(string file, string source)>> classes = packets
                    .Select(static (packets, ct) => 
                    {
                        var output = new ConcurrentBag<(string, string)>();
                        Parallel.ForEach(packets, packet => PacketGenerator.GenerateClass(packet, output));
                        return output.ToImmutableArray();
                    });

            initContext.RegisterSourceOutput(classes, (context, classes) =>
            {
                foreach (var packet in classes)
                    context.AddSource(packet.file, packet.source);
            });
        }

        private static ImmutableArray<Packet> ParseProtocolSpec(string protocol)
        {
            var sr = new StringReader(protocol);
            List<Packet> packets = new();
            Packet? p;
            while ((p = ParsePacket(sr)) is not null)
                packets.Add(p.Value);
            return packets.ToImmutableArray();
        }

        private static Packet? ParsePacket(StringReader sr)
        {
            Packet packet = new();
            string line;

            // Read packet name
            if (!ReadUntil("## ", out line!))
                return null;
            packet.Name = MakeMethodName(line.Substring(2));

            // Read id and length
            if (!ReadUntil("| Packet ID", out line!))
                throw new Exception($"Unexpected EOF while reading id for packet {packet.Name}");
            // |Packet ID:  |  0       |
            packet.Id = Byte.Parse(
                line.Split('|')[2]
                .Trim()
                );
            
            line = sr.ReadLine();
            // |Total Size: | 13 bytes |
            packet.Length = Int32.Parse(
                line.Split('|')[2]
                .Trim()
                .Split(' ')[0]);

            // Read packet fields
            ReadUntil("| Field Name", out line!);
            sr.ReadLine();
            while ((line = sr.ReadLine()) != "")
            {
                // Comments
                if (line.StartsWith("#"))
                    continue;

                string[] parts = line.Split('|')
                    .Select(s => s.Trim())
                    .ToArray();
                
                var field = new Field
                {
                    Name = MakeMethodName(parts[1]),
                    Type = parts[2]
                };

                if (!String.IsNullOrEmpty(parts[5]))
                    field.ActualType = parts[5];
                else
                    field.ActualType = field.Type;

                packet.Fields.Add(field);
            }

            return packet;

            bool ReadUntil(string sequence, out string? line)
            {
                while (true)
                {
                    line = sr.ReadLine();
                    if (line is null)
                        return false;
                    if (line.StartsWith(sequence))
                        return true;
                }
            }
        }

        private static string MakeMethodName(string s)
        {
            return String.Join("", CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s).Split(' '))
                        .Replace("ID", "Id")
                        .Replace("HP", "Hp");
        }

        private static void GenerateClass(Packet packet, ConcurrentBag<(string, string)> output)
        {
            try
            {
                var source = new StringBuilder();

                source.AppendLine("using System;");
                source.AppendLine("using System.Runtime.CompilerServices;");
                source.AppendLine();

                source.AppendLine("namespace SharpSpades.Api.Net.Packets;");
                source.Append($@"
[CompilerGenerated]
public partial struct {packet.Name} : IPacket
{{");

                source.AppendLine();

                // Metadata
                Indent(source, 1);
                source.AppendLine($"public byte Id => {packet.Id};");
                Indent(source, 1);
                source.AppendLine($"public int Length => {packet.Length - 1};");
                source.AppendLine();

                // Fields
                foreach (var field in packet.Fields)
                {
                    try
                    {
                        Indent(source, 1);
                        source.AppendLine(field.GenerateProperty().Replace("\t", "    "));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to generate field {packet.Name}.{field.Name}: {ex.GetType().Name}: {ex.Message}", ex);
                    }
                }

                var read = new StringBuilder();
                var write = new StringBuilder();
                int readOffset = 0;
                int writeOffset = 0;
                foreach (var field in packet.Fields)
                {
                    read.AppendLine(field.GenerateRead(ref readOffset));
                    write.AppendLine(field.GenerateWrite(ref writeOffset));
                }

                source.Append(@"
    public void Read(ReadOnlySpan<byte> buffer)
    {
");
                Indent(source, 2);
                source.Append(String.Join("\n        ", read.ToString().TrimEnd().Split('\n')));

                // Length assert
                source.AppendLine();
                Indent(source, 2);
                source.Append($@"
        if (buffer.Length > {packet.Length - 1})
            throw new ArgumentException(""Packet is too long"");
");

                source.AppendLine("    }");

                source.Append(@"
    public void Write(Span<byte> buffer)
    {
");
                Indent(source, 2);
                source.Append(String.Join("\n        ", write.ToString().TrimEnd().Split('\n')));
                source.AppendLine("\n    }");

                source.Append("}\n");
                output.Add(($"{packet.Name}.g.cs", source.ToString()));
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to generate packet {packet.Name}:\n{ex}", ex);
            }

            void Indent(StringBuilder source, int indent)
            {
                for (int i = 0; i < indent; i++)
                    source.Append("    ");
            }
        }
    }
}