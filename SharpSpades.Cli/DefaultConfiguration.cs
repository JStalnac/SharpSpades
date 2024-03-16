namespace SharpSpades
{
    public class DefaultConfiguration
    {
        public ushort Port { get; set; } = 32887;

        public LoggingConfiguration LogLevels { get; set; } = new();
        public string MapName { get; set; } = "classicgen.vxl";

        public float Tps { get; set; } = 60f;
    }

    public class LoggingConfiguration
    {
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