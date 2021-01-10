using Nett;
using System;

namespace SharpSpades
{
    public class ServerConfiguration
    {
        [TomlComment(" The port that the server will listen on")]
        public ushort Port { get; set; } = 32887;
        public LoggingConfiguration LogLevels { get; set; } = new();
    }

    public class LoggingConfiguration
    {
        [TomlComment(@" The default log level. Defaults to 'information'")]
        public string Default { get; set; } = "information";
        public string[] Trace { get; set; } = Array.Empty<string>();
        public string[] Debug { get; set; } = Array.Empty<string>();
        public string[] Information { get; set; } = Array.Empty<string>();
        public string[] Warning { get; set; } = Array.Empty<string>();
        public string[] Error { get; set; } = Array.Empty<string>();
        public string[] Fatal { get; set; } = Array.Empty<string>();

        public string LogFile { get; set; } = "logs/.log";
        public bool RollDaily { get; set; } = true;
    }
}
