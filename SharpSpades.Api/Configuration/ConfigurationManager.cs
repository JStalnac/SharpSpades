using Microsoft.Extensions.Logging;
using SharpSpades.Api.Configuration.Attributes;
using SharpSpades.Api.Utils;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Tommy;

#nullable enable

namespace SharpSpades.Api.Configuration
{
    public sealed class ConfigurationManager
    {
        // Not constant for testing
        internal static int tableMaxLevels = 1;

        private readonly ILogger<ConfigurationManager> logger;

        public ConfigurationManager(ILogger<ConfigurationManager> logger)
        {
            Throw.IfNull(logger);
            this.logger = logger;
        }

        /// <summary>
        /// Saves the provided object <paramref name="obj"/> to TOML and then writes it to <paramref name="output"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">Object to save</param>
        /// <param name="output">TextWriter to write the TOML into</param>
        public void Save<T>(T obj, TextWriter output) where T : IConfiguration
        {
            if (obj is null)
                throw new ArgumentNullException(nameof(obj));
            Throw.IfNull(output);
            
            var type = typeof(T);
            ThrowIfTypeNotValid(type);

            var table = LoadTable(obj, 0, null,
                type.GetCustomAttribute<CommentAttribute>()?.Comment);
            table.WriteTo(output);
        }

        /// <summary>
        /// Loads the public properties of a CLR type to a <see cref="TomlTable"/>
        /// <br></br>
        /// <b>Should not be used directly, use <see cref="Save{T}(T, TextWriter)"/> instead.</b>
        /// </summary>
        /// <param name="obj">Object to load properties from</param>
        /// <param name="level">What level we are in in the current TOML table. Used for stopping</param>
        /// <param name="parent">The parent properties names as string. Used for logging</param>
        /// <param name="comment">Comment that the TOML table should have</param>
        /// <returns></returns>
        internal TomlTable LoadTable(object obj, int level, string? parent, string? comment)
        {
            Throw.IfNull(obj);
            var type = obj.GetType();

            // Gather results here
            var result = new TomlTable
            {
                Comment = comment
            };

            var properties = GetProperties(type);

            // Load all values of properties
            var values = properties.Select(p => (n: p.Name.ToSnakeCase(), p,
                c: p.GetCustomAttribute<CommentAttribute>()?.Comment,
                g: p.GetGetMethod()!));

            // Process data to TOML table
            foreach (var v in values)
            {
                object value;
                try
                {
                    value = v.g.Invoke(obj, null)!;
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Getter for property {(parent is null ? "" : parent + ".")}{v.p.Name} threw an unhandled exception: {ex.Message}");
                    continue;
                }

                logger.LogTrace($"Loading property {(parent is null ? "" : parent + ".")}{v.p.Name}");
                if (value is IEnumerable e && value is not string)
                {
                    // Collection
                    var array = new TomlArray();
                    foreach (object o in e)
                    {
                        var node = GetNode(o, v.c);
                        if (node is not null)
                            array.Add(node);
                        else
                            logger.LogDebug($"Type {o.GetType()} in {(parent is null ? "" : parent + ".")}{v.p.Name} is not supported for TOML array");
                    }
                    result.Add(v.n, array);
                }
                else
                {
                    var node = GetNode(value, v.c);
                    if (node is not null)
                        result.Add(v.n, node);
                    else
                    {
                        // Other type
                        if (level >= tableMaxLevels)
                        {
                            // Parent should be set at this point
                            logger.LogDebug($"Not loading custom property {(parent is null ? "" : parent + ".")}{v.p.Name} because table level limit hit");
                            continue;
                        }

                        if (value is null)
                        {
                            logger.LogDebug($"Property {v.p.Name} is null. Skipping");
                            continue;
                        }

                        if (v.p.GetCustomAttribute<TableAttribute>() is not null)
                            result.Add(v.n, LoadTable(value, level + 1, parent is null ? v.p.Name : $"{parent}.{v.p.Name}", v.p.GetCustomAttribute<CommentAttribute>()?.Comment));
                        else
                            logger.LogDebug($"Property {v.p.Name} is a custom property but not marked as table. Skipping");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a value for the TomlNode from the object. Returns null if the object is not null
        /// </summary>
        /// <param name="value"></param>
        /// <param name="comment"></param>
        /// <returns></returns>
        internal static TomlNode? GetNode(object? value, string? comment)
            => value switch
            {
                string s => new TomlString
                {
                    Comment = comment,
                    Value = s
                },
                int i => new TomlInteger
                {
                    Comment = comment,
                    Value = i
                },
                float f => new TomlFloat
                {
                    Comment = comment,
                    Value = f
                },
                bool b => new TomlBoolean
                {
                    Comment = comment,
                    Value = b
                },
                DateTime dt => new TomlDateTime
                {
                    // TODO: Could add other properties
                    Comment = comment,
                    Value = dt
                },
                _ => null
            };

        public T Load<T>(TextReader input) where T : IConfiguration
        {
            Throw.IfNull(input);

            TomlTable table;
            using (var parser = new TOMLParser(input))
            {
                if (!parser.TryParse(out var toml, out var errors))
                    logger.LogWarning($"Failed to parse TOML:\n{String.Join("\n", errors.Select(e => $"L:{e.Line}C:{e.Column}: {e.Message}"))}");
                table = (TomlTable)toml;
            }



            throw new NotImplementedException();
        }

        internal static PropertyInfo[] GetProperties(Type type)
            => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetGetMethod() != null && p.GetSetMethod() != null).ToArray();

        internal static void ThrowIfTypeNotValid(Type type)
        {
            if (type.IsNested || type.IsGenericType)
                throw new ArgumentException("Type must not be nested or generic");
        }
    }
}
