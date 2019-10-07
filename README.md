# VMware AutoShutdown
This docker image will monitor a given MQTT topic for given payload and will initiate a gracefull shutdown of an ESXi server (and VM's) upon receiving that payload (after given timeout).

__Usecase example__

I use it to monitor my powerline, upon failure an ESP will publish a payload to a MQTT topic and after 5 minutes my ESXi server will shutdown to prevent dataloss (learned from experience)

## How to use
Run the docker image as followed
```bash
docker run -d --name vmware-auto-shutdown bluewalk/vmware-auto-shutdown [-e ...]
```
You can specify configuration items using environment variables as displayed below, e.g.
```
docker run -d --name vmware-auto-shutdown bluewalk/vmware-auto-shutdown -e TIMEOUT_SECONDS=180 -e MQTT_HOST=192.168.1.2
```

You can alter the `log4net` settings by mapping a local `log4net.config` file to `/app/log4.net.config`, e.g.
```
docker run -d --name vmware-auto-shutdown bluewalk/vmware-auto-shutdown [-e ...] -v [configfile]:/app/log4net.config
```

## Environment variables
|Environment variable|Description|Default when empty|
|-|-|-|
|MQTT_HOST|MQTT Host|`127.0.0.1`|
|MQTT_PORT|MQTT Port|`1883`|
|MQTT_USERNAME|MQTT Username|null|
|MQTT_PASSWORD|MQTT Password|null|
|MQTT_SHUTDOWN_TOPIC|MQTT Topic to watch|`bluewalk/shutdown`|
|MQTT_SHUTDOWN_PAYLOAD|MQTT Payload to match for shutdown|`yes`|
|MQTT_REPORT_TOPIC|MQTT Topic to report current status|`bluewalk/shutdown/report`|
|ESXI_USERNAME|ESXi username|null|
|ESXI_PASSWORD|ESXi password|null|
|ESXI_IP|ESXi IP address|null|
|ESXI_TIMEOUT|ESXi shutdown VM's timeout, after passing will shutdown host|`300`|
|ESXI_VMTOSKIP|ESXi VM's to skip during shutdown|null|
|TIMEOUT_SECONDS|Timeout in seconds before initiating shutdown (grace period)|`300`|
