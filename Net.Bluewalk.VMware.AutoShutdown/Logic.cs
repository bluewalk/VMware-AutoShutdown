using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Net.Bluewalk.VMware.AutoShutdown.Models;
using Timer = System.Timers.Timer;

namespace Net.Bluewalk.VMware.AutoShutdown
{
    public class Logic : IHostedService
    {
        private readonly Config _config;
        private readonly IManagedMqttClient _mqttClient;
        private readonly Timer _timeoutTimer;
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        public Logic(ILogger<Logic> logger, IOptions<Config> config)
        {
            _logger = logger;
            _config = config.Value;

            _mqttClient = new MqttFactory().CreateManagedMqttClient();
            _mqttClient.UseApplicationMessageReceivedHandler(MqttClientOnApplicationMessageReceived);
            _mqttClient.UseConnectedHandler(async e =>
            {
                _logger.LogInformation("Connected to MQTT server");

                await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(_config.Mqtt.ShutdownTopic)
                    .Build());
            });
            _mqttClient.UseDisconnectedHandler(e =>
            {
                if (e.Exception != null && !e.ClientWasConnected)
                    _logger.LogError(e.Exception, "Unable to connect to MQTT server");

                if (e.Exception != null && e.ClientWasConnected)
                    _logger.LogError("Disconnected from connect to MQTT server with error", e.Exception);

                if (e.Exception == null && e.ClientWasConnected)
                    _logger.LogInformation("Disconnected from MQTT server");
            });

            _timeoutTimer = new Timer(_config.TimeoutSeconds * 1000);
            _timeoutTimer.Elapsed += async (sender, args) =>
            {
                _timeoutTimer.Stop();

                await Report("SHUTDOWN_INITIATED");

                _logger.LogInformation("Starting VMware PowerCLI shutdown script");
                var proc = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        Arguments = "-c /app/shutdown.ps1",
                        EnvironmentVariables =
                        {
                            {"esxiusername", _config.Esxi.Username},
                            {"esxipassword", _config.Esxi.Password},
                            {"esxiip", _config.Esxi.Ip},
                            {"esxitimeout", _config.Esxi.Timeout},
                            {"esxivmnametoskip", _config.Esxi.VmNameToSkip}
                        },
                        FileName = "/usr/bin/pwsh",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };
                proc.OutputDataReceived += (o, eventArgs) => _logger.LogInformation(eventArgs.Data);
                proc.ErrorDataReceived += (o, eventArgs) => _logger.LogError(eventArgs.Data);

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
            };
        }

        /// <summary>
        /// Process MQTT messages
        /// </summary>
        /// <param name="e"></param>
        private async Task MqttClientOnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            _logger.LogInformation("Received MQTT message on topic {0}", e.ApplicationMessage.Topic);

            if (!e.ApplicationMessage.Topic.Equals(_config.Mqtt.ShutdownTopic,
                StringComparison.InvariantCultureIgnoreCase)) return;

            var message = e.ApplicationMessage.ConvertPayloadToString();
            await ControlCountdown(message.Equals(_config.Mqtt.ShutdownPayload,
                StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Control the countdown
        /// </summary>
        /// <param name="enabled"></param>
        /// <returns></returns>
        private async Task ControlCountdown(bool enabled)
        {
            if (enabled && !_timeoutTimer.Enabled)
            {
                await Report("SHUTDOWN_COUNTDOWN_INITIATED");
                _timeoutTimer.Start();
            }
            else if (!enabled && _timeoutTimer.Enabled)
            {
                await Report("SHUTDOWN_COUNTDOWN_ABORTED");
                _timeoutTimer.Stop();
            }
        }

        /// <summary>
        /// Method to report the status of the process
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task Report(string message)
        {
            _logger.LogInformation("Reporting status {0}", message);

            var msg = new MqttApplicationMessageBuilder()
                .WithTopic(_config.Mqtt.ReportTopic)
                .WithPayload(message)
                .WithExactlyOnceQoS()
                .Build();

            await _mqttClient.PublishAsync(msg);
        }

        /// <summary>
        /// Start logic
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting logic");

            var clientOptions = new MqttClientOptionsBuilder()
                .WithClientId($"VMwareAutoShutdown-{Environment.MachineName}-{Environment.UserName}")
                .WithTcpServer(_config.Mqtt.Host, _config.Mqtt.Port);

            if (!string.IsNullOrEmpty(_config.Mqtt.Username))
                clientOptions = clientOptions.WithCredentials(_config.Mqtt.Username,
                    _config.Mqtt.Password);

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(clientOptions);

            _logger.LogInformation("Connecting to MQTT");
            await _mqttClient.StartAsync(managedOptions.Build());
        }

        /// <summary>
        /// Stop logic
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Disconnecting from MQTT server");
            await _mqttClient?.StopAsync();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {

        }
    }
}