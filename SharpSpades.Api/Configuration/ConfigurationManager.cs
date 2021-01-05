using Microsoft.Extensions.Logging;
using SharpSpades.Api.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;

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
        private readonly IDisposable disposable;
        private readonly ILogger logger;
        private readonly FileSystemWatcher fileSystemWatcher;
        private readonly Dictionary<string, ConfigurationFile> configurations = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationManager"/> class.
        /// </summary>
        /// <param name="rootPath">The root path in the filesystem that the <see cref="ConfigurationManager"/> will be listening on. If the value is null the current directory of the process will be used.</param>
        /// <param name="logger">The logger that log messages will written to by the <see cref="ConfigurationManager"/> and its configurations.</param>
        public ConfigurationManager(string? rootPath, ILogger logger)
        {
            Throw.IfNull(logger, nameof(logger));

            if (rootPath == ".")
                throw new ArgumentException($"'{rootPath}' is not a valid path.");

            // Will fail in most cases if the path is not valid
            this.rootPath = !String.IsNullOrEmpty(rootPath) ? Path.Combine(Directory.GetCurrentDirectory(), rootPath) : Directory.GetCurrentDirectory();
            fileSystemWatcher = new(this.rootPath)
            {
                NotifyFilter = NotifyFilters.LastWrite,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            this.logger = logger;
            // Listen for events
            disposable = Observable
              .FromEventPattern<FileSystemEventArgs>(fileSystemWatcher, "Changed")
              .Throttle(TimeSpan.FromSeconds(1))
              .Subscribe(x =>
              {
                  if (configurations.TryGetValue(x.EventArgs.FullPath, out var config))
                  {
                      logger.LogInformation($"Detected changes in '{x.EventArgs.FullPath}'. Updating...");
                      try
                      {
                          config.Reload();
                      }
                      catch (Exception ex)
                      {
                          logger.LogError(ex, $"Failed to update file '{x.EventArgs.FullPath}'");
                      }
                      logger.LogInformation($"Updated '{x.EventArgs.FullPath}'");
                  }
              });
        }

        /// <summary>
        /// Adds a new configuration that will be kept track of by the <see cref="ConfigurationManager"/>.
        /// </summary>
        /// <param name="path">The path to the configuration file the configuration should be read from.</param>
        /// <param name="file">The configuration that will be added.</param>
        /// <exception cref="ArgumentException">The provided path is not a relative path.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null or empty or <paramref name="file"/> is null.</exception>
        /// <exception cref="InvalidOperationException">A configuration with the same path is alreay added to the <see cref="ConfigurationManager"/>.</exception>
        public void AddConfiguration(string path, ConfigurationFile file)
        {
            // Zero-length string won't be a proper path to a file.
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            Throw.IfNull(file, nameof(file));

            if (Path.IsPathRooted(path))
                throw new ArgumentException("The path must be a relative path", nameof(path));

            var fileInfo = new FileInfo(Path.Combine(rootPath, path));
            if (configurations.ContainsKey(fileInfo.FullName))
                throw new InvalidOperationException("The file path is already used by an existing configuration");
            file.File = fileInfo;
            file.logger = logger;

            // Initialize the file
            file.Reload();

            // Add the file here so that it won't get updated possibly
            configurations.Add(fileInfo.FullName, file);
        }

        /// <summary>
        /// Gets a configuration with the specified path. The configuration must have been added to the <see cref="ConfigurationManager"/>.
        /// </summary>
        /// <param name="path">The path for the configuration. The path can be absolute or relative.</param>
        /// <returns>The configuration file at the specified path.</returns>
        public ConfigurationFile GetConfiguration(string path)
        {
            string p = null!;
            if (Path.IsPathRooted(path))
                p = path;
            p = Path.Combine(rootPath, path);
            if (configurations.TryGetValue(p, out var config))
                return config;
            throw new KeyNotFoundException($"Could not find a configuration with the name '{path}'");
        }
    }
}
