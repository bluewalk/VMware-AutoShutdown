using Net.Bluewalk.DotNetEnvironmentExtensions;

namespace Net.Bluewalk.VMware.AutoShutdown.Models
{
    public class Config
    {
        public Mqtt Mqtt { get; set; }
        public Esxi Esxi { get; set; }

        [EnvironmentVariable(Name = "TIMEOUT_SECONDS", Default = 300)]
        public int TimeoutSeconds { get; set; }
    }
}