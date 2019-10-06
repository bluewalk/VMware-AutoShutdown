using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Net.Bluewalk.DotNetEnvironmentExtensions;
using Net.Bluewalk.VMware.AutoShutdown.Models;

namespace Net.Bluewalk.VMware.AutoShutdown
{
    public class Logic
    {
        private readonly Config _config;
        private readonly IManagedMqttClient _mqttClient;
        private readonly Timer _timeoutTimer;
        
        public Logic()
        {
            _config = (Config)typeof(Config).FromEnvironment();
            
            _mqttClient = new MqttFactory().CreateManagedMqttClient();
            _mqttClient.UseApplicationMessageReceivedHandler(MqttClientOnApplicationMessageReceived);
            _mqttClient.UseConnectedHandler(async e =>
                {
                    await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(_config.Mqtt.ShutdownTopic)
                        .Build());
                });

            _timeoutTimer = new Timer(_config.TimeoutSeconds);
            _timeoutTimer.Elapsed += (sender, args) =>
            {
                _timeoutTimer.Stop();
                
                var proc = new ProcessStartInfo()
                {
                    Arguments = "-c shutdown.ps1",
                    EnvironmentVariables =
                    {
                        {"esxiusername", _config.Esxi.Username},
                        {"esxipassword", _config.Esxi.Password},
                        {"esxiip", _config.Esxi.Ip},
                        {"esxitimeout", _config.Esxi.Timeout},
                        {"esxivmnametoskip", _config.Esxi.VmNameToSkip}
                    },
                    FileName = "/usr/bin/pwsh"
                };
                Process.Start(proc);
            };
        }

        /// <summary>
        /// Process MQTT messages
        /// </summary>
        /// <param name="e"></param>
        private void MqttClientOnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            if (!e.ApplicationMessage.ToString()
                .Equals(_config.Mqtt.ShutdownTopic, StringComparison.InvariantCultureIgnoreCase)) return;
            
            var message = e.ApplicationMessage.ConvertPayloadToString();
            if (message.Equals(_config.Mqtt.ShutdownPayload, StringComparison.InvariantCultureIgnoreCase))
                _timeoutTimer.Start();
            else
                _timeoutTimer.Stop();
        }

        /// <summary>
        /// Start logic
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            var clientOptions = new MqttClientOptionsBuilder()
                .WithClientId($"VMwareAutoShutdown-{Environment.MachineName}-{Environment.UserName}")
                .WithTcpServer(_config.Mqtt.Host, _config.Mqtt.Port);

            if (!string.IsNullOrEmpty(_config.Mqtt.Username))
                clientOptions = clientOptions.WithCredentials(_config.Mqtt.Username,
                    _config.Mqtt.Password);

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(clientOptions);

            await _mqttClient.StartAsync(managedOptions.Build());
        }
        
        /// <summary>
        /// Stop logic
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {
            await _mqttClient?.StopAsync();
        }
    }
}