using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tommy;

#nullable enable

namespace SharpSpades.Api.Configuration
{
    public class ConfigurationFile
    {
        internal ILogger? logger;
        private FileInfo? file;
        internal FileInfo? File
        {
            get => file;
            set
            {
                lock (ioAccesslock)
                    file = value;
            }
        }
        private readonly object ioAccesslock = new();
        private TomlTable? Toml { get; set; }
        private Table Data { get; }

        internal ConfigurationFile(Table data)
        {
            Data = data;
        }

        public TomlTable GetConfiguration()
        {
            if (Toml is null)
                throw new InvalidOperationException($"The {nameof(ConfigurationFile)} must be added to a {nameof(ConfigurationManager)} before accessing its configuration");
            return Toml;
        }

        internal void Reload()
        {
            lock (ioAccesslock)
            {
                if (File is null)
                    return;
                File.Refresh();

                if (!File.Exists)
                {
                    logger.LogDebug($"Configuration file '{File.FullName}' was not found. Creating a new file...");
                    if (Toml is null)
                        Toml = WriteToToml(Data);
                    using var stream = File.Open(FileMode.CreateNew, FileAccess.Write);
                    using var writer = new StreamWriter(stream);
                    Toml.WriteTo(writer);
                    logger.LogDebug("Created new configuration file");
                }
                else
                {
                    logger.LogInformation($"Loading changes from file '{File.FullName}'");
                    // There is new data to load
                    logger.LogDebug("Opening file stream");
                    using var stream = File.OpenRead();
                    using var reader = new StreamReader(stream);
                    using var parser = new TOMLParser(reader);
                    logger.LogDebug("Parsing TOML");
                    if (!parser.TryParse(out var newToml, out var errors))
                        logger.LogWarning($@"Failed to load TOML from file '{File.FullName}'. Continuing to load changes. Errors:{"\n"}{String.Join("\n",
                            errors.Select(e => $"{e.Line + 1}:{e.Column + 1}: {e.Message}"))}");

                    var newTable = (TomlTable)newToml;

                    logger.LogDebug("Validating new configuration");

                    // Validate the new data
                    if (!ValidateTable(newTable, Data, new Stack<string>()))
                        return;

                    logger.LogInformation($"Done loading changes from file '{File.FullName}'");

                    // Assign
                    Toml = newTable;
                }
            }
        }

        private bool ValidateTable(TomlTable newToml, Table table, Stack<string> parts)
        {
            // Validate fields
            foreach (var field in table.Fields)
            {
                parts.Push(field.Name);

                // Check if the value is missing
                var node = newToml[field.Name];
                if (!node.HasValue)
                {
                    if (field.Required)
                    {
                        logger.LogWarning($"Field '{GetPartString()}' is missing but is required. Discarding the configuration");
                        return false; // Don't bother about the parts stack.
                    }
                    // Nothing to validate
                    continue;
                }

                // Check type
                if (node.GetType() != field.Type)
                {
                    logger.LogWarning($"Field '{GetPartString()}' is type {field.Type.Name} but got {node.GetType().Name} instead. Discarding the configuration");
                    return false;
                }

                // Validate the new value
                if (field.Validator is not null)
                {
                    try
                    {
                        if (!field.Validator(node))
                        {
                            logger.LogWarning($"Validator for field '{GetPartString()}' failed. Discarding the configuration");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"Validator for field '{GetPartString()}' threw an unhandled exception. Assuming the value is good. Exception:\n{ex}");
                    }
                }
                parts.Pop();
            }

            // Validate tables
            foreach (var t in table.Tables)
            {
                parts.Push(t.Name);
                // Does the table exist
                if (newToml[t.Name] is TomlTable tomlTable)
                {
                    // Is the table valid
                    if (!ValidateTable(tomlTable, t, parts))
                        return false;
                }
                // Missing
                else if (t.Required)
                {
                    logger.LogWarning($"Table '{GetPartString()}' is missing but is required. Discarding the configuration");
                    return false;
                }
                parts.Pop();
            }

            return true;

            string GetPartString()
                => String.Join(".", parts.Reverse());
        }

        private TomlTable WriteToToml(Table table)
        {
            var toml = new TomlTable();
            foreach (var t in table.Tables)
                toml.Add(t.Name, WriteToToml(t));
            foreach (var field in table.Fields)
                toml.Add(field.Name, CopyValue(field.InitialValue)!);
            return toml;
        }

        private static TomlNode CopyValue(TomlNode node)
        {
            if (node is TomlArray a)
            {
                var a2 = new TomlArray();
                a2.AddRange(a.RawArray);
                return a2;
            }
            return node switch
            {
                TomlString s => s.Value,
                TomlInteger i => i.Value,
                TomlFloat f => f.Value,
                TomlBoolean b => b.Value,
                TomlDateTime dt => dt.Value,
                _ => null! // TomlTable, should never happen
            };
        }
            
    }
}
