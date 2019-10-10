namespace Net.Bluewalk.VMware.AutoShutdown.Models
{
    public class Esxi
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Ip { get; set; }
        public string Timeout { get; set; }
        public string VmNameToSkip { get; set; }
    }
}