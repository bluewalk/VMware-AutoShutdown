namespace Net.Bluewalk.VMware.AutoShutdown.Models
{
    public class Config
    {
        public Mqtt Mqtt { get; set; }
        public Esxi Esxi { get; set; }

        public int TimeoutSeconds { get; set; }

        public Config()
        {
            TimeoutSeconds = 300;

            Mqtt = new Mqtt
            {
                Host = "127.0.0.1",
                Port = 1883,
                ReportTopic = "bluewalk/shutdown/report",
                ShutdownTopic = "bluewalk/shutdown",
                ShutdownPayload = "yes"
            };

            Esxi = new Esxi
            {
                Timeout = "300"
            };
        }
    }
}