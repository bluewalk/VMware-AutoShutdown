namespace Net.Bluewalk.VMware.AutoShutdown.Models
{
    public class Mqtt
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        
        public string ShutdownTopic { get; set; }
        public string ShutdownPayload { get; set; }
        public string ReportTopic { get; set; }
    }
}