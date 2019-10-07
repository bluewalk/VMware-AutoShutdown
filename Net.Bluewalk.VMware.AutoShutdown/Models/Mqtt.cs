using Net.Bluewalk.DotNetEnvironmentExtensions;

namespace Net.Bluewalk.VMware.AutoShutdown.Models
{
    public class Mqtt
    {
        [EnvironmentVariable(Name = "MQTT_HOST", Default = "127.0.0.1")]
        public string Host { get; set; }
        [EnvironmentVariable(Name = "MQTT_PORT", Default = 1883)]
        public int Port { get; set; }
        [EnvironmentVariable(Name = "MQTT_USERNAME")]
        public string Username { get; set; }
        [EnvironmentVariable(Name = "MQTT_PASSWORD")]
        public string Password { get; set; }
        
        [EnvironmentVariable(Name = "MQTT_SHUTDOWN_TOPIC", Default = "bluewalk/shutdown")]
        public string ShutdownTopic { get; set; }
        [EnvironmentVariable(Name = "MQTT_SHUTDOWN_PAYLOAD", Default = "yes")]
        public string ShutdownPayload { get; set; }
        [EnvironmentVariable(Name = "MQTT_REPORT_TOPIC", Default = "bluewalk/shutdown/report")]
        public string ReportTopic { get; set; }
    }
}