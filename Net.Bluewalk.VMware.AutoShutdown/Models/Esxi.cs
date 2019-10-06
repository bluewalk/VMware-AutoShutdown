using Net.Bluewalk.DotNetEnvironmentExtensions;

namespace Net.Bluewalk.VMware.AutoShutdown.Models
{
    public class Esxi
    {
        [EnvironmentVariable(Name = "ESXI_USERNAME")]
        public string Username { get; set; }
        [EnvironmentVariable(Name = "ESXI_PASSWORD")]
        public string Password { get; set; }
        [EnvironmentVariable(Name = "ESXI_IP")]
        public string Ip { get; set; }
        [EnvironmentVariable(Name = "ESXI_TIMEOUT")]
        public string Timeout { get; set; }
        [EnvironmentVariable(Name = "ESXI_VMTOSKIP")]
        public string VmNameToSkip { get; set; }
        
    }
}