using Microsoft.Extensions.Logging;
using SharpSpades.Api.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using Tommy;

#nullable enable

namespace SharpSpades.Api.Configuration
{
    /// <summary>
    /// Keeps track of configurations and updates them when their files are updated.
    /// This class cannot be inherited.
    /// </summary>
    public sealed class ConfigurationManager
    {
        private readonly string rootPath;
        private readonly ILogger<ConfigurationManager> logger;
        private readonly FileSystemWatcher fileSystemWatcher;
        private readonly Dictionary<FileInfo, ConfigurationFile> configurations = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationManager"/> class.
        /// </summary>
        /// <param name="rootPath">The root path in the filesystem that the <see cref="ConfigurationManager"/> will be listening on.</param>
        public ConfigurationManager(string rootPath, ILogger<ConfigurationManager> logger)
        {
            Throw.IfNull(rootPath, nameof(rootPath));
            Throw.IfNull(logger, nameof(logger));
            // Will fail if the path is not valid
            this.rootPath = Path.Combine(Directory.GetCurrentDirectory(), rootPath);
            fileSystemWatcher = new(this.rootPath);
            fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            this.logger = logger;
        }
        
        /// <summary>
        /// Adds a new configuration that will be kept track of by the <see cref="ConfigurationManager"/>.
        /// </summary>
        /// <param name="path">The path to the configuration file the configuration should be read from.</param>
        /// <param name="file">The configuration that will be added.</param>
        public void AddConfiguration(string path, ConfigurationFile file)
        {
            Throw.IfNull(path, nameof(path));
            Throw.IfNull(file, nameof(file));
            if (Path.IsPathRooted(path))
                throw new ArgumentException("The path must be a relative path", nameof(path));
            var fileInfo = new FileInfo(Path.Combine(rootPath, path));
            if (configurations.ContainsKey(fileInfo))
                throw new InvalidOperationException("The file path is already used by an existing configuration");
            configurations.Add(fileInfo, file);
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var fileInfo = new FileInfo(e.FullPath);
            if (configurations.TryGetValue(fileInfo, out var config))
            {
                // TODO: Read data
                // TODO: Update configuration
            }
        }
        
        private void UpdateConfiguration(ConfigurationFile config, TomlTable data)
        {
            throw new NotImplementedException();
        }

        private void InitializeConfiguration(FileInfo file, ConfigurationFile config)
        {
            throw new NotImplementedException();
        }
    }
}
