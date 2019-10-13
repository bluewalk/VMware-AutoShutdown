using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Net.Bluewalk.VMware.AutoShutdown.Models;
using Renci.SshNet;
using Timer = System.Timers.Timer;

namespace Net.Bluewalk.VMware.AutoShutdown
{
    public class Logic : IHostedService
    {
        private readonly Config _config;
        private readonly IManagedMqttClient _mqttClient;
        private readonly Timer _timeoutTimer;
        private readonly ILogger _logger;
        private readonly IHostEnvironment _hostEnvironment;

        /// <summary>
        /// Constructor
        /// </summary>
        public Logic(ILogger<Logic> logger, IOptions<Config> config, IHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _config = config.Value;
            _hostEnvironment = hostEnvironment;

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

                InitiateShutdown();
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
        /// Initiate shutdown
        /// </summary>
        private async void InitiateShutdown()
        {
            await Report("SHUTDOWN_INITIATED");

            // Keyboard auth for ESXi server
            var kbAuth = new KeyboardInteractiveAuthenticationMethod(_config.Esxi.Username);
            kbAuth.AuthenticationPrompt += (sender, args) =>
            {
                foreach (var prompt in args.Prompts)
                    if (prompt.Request.IndexOf("Password:", StringComparison.InvariantCultureIgnoreCase) != -1)
                        prompt.Response = _config.Esxi.Password;
            };
            var sshInfo = new ConnectionInfo(_config.Esxi.Ip, _config.Esxi.Username, kbAuth);

            // Upload shutdown script
            _logger.LogInformation("Uploading shutdown script to /tmp");
            using (var sftp = new SftpClient(sshInfo))
            {
                sftp.ErrorOccurred += (sender, args) =>
                    _logger.LogError(args.Exception, "An error occurred when connecting to ESXi server");

                sftp.Connect();
                sftp.ChangeDirectory("/tmp");

                await using (var fileStream =
                    File.OpenRead(Path.Combine(_hostEnvironment.ContentRootPath, "shutdown.sh")))
                {
                    sftp.UploadFile(fileStream, "shutdown.sh", true);
                    _logger.LogInformation("Upload complete");
                }

                sftp.Disconnect();
            }

            // Execute shutdown script
            _logger.LogInformation("Executing shutdown script");
            using (var sshClient = new SshClient(sshInfo))
            {
                sshClient.Connect();
                if (!sshClient.IsConnected) return;

                using (var cmd = sshClient.CreateCommand("chmod +x /tmp/shutdown.sh && /tmp/shutdown.sh"))
                {
                    cmd.Execute();

                    _logger.LogInformation("SSH command> {0}", cmd.CommandText);
                    _logger.LogInformation("SSH result> '{0}' ({1})", cmd.Result, cmd.ExitStatus);

                    if (cmd.ExitStatus == 0)
                        await Report("SHUTDOWN_COMPLETED");
                }

                sshClient.Disconnect();
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
    }
}