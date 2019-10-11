# VMware AutoShutdown
This docker image will monitor a given MQTT topic for given payload and will initiate a gracefull shutdown of an ESXi server (and VM's) upon receiving that payload (after given timeout).

__Usecase example__

I use it to monitor my powerline, upon failure an ESP will publish a payload to a MQTT topic and after 5 minutes my ESXi server will shutdown to prevent dataloss (learned from experience)

## How to use
Run the docker image as followed
```bash
docker run -d --name vmware-auto-shutdown [-e ...] bluewalk/vmware-auto-shutdown
```
You can specify configuration items using environment variables as displayed below, e.g.
```
docker run -d --name vmware-auto-shutdown -e Config:TimeoutSeconds=180 -e Config:Mqtt:Host=192.168.1.2 bluewalk/vmware-auto-shutdown
```
If running on a Raspberry Pi you can use the GPIO to minotor the powerine (eg with an 5v adapter in a non UPS backed socket), when falling (eg power-loss) this will initate the countdown. In order for this to work you need to map /sys to the container.
```
docker run -d --name vmware-auto-shutdown -v /sys:/sys -e Config:TimeoutSeconds=180 -e Config:Mqtt:Host=192.168.1.2 -e Config:GpioPin=5 bluewalk/vmware-auto-shutdown
```

## Environment variables
|Environment variable|Description|Default when empty|
|-|-|-|
|`Config:Mqtt:Host`|MQTT Host|`127.0.0.1`|
|`Config:Mqtt:Port`|MQTT Port|`1883`|
|`Config:Mqtt:Username`|MQTT Username|null|
|`Config:Mqtt:Password`|MQTT Password|null|
|`Config:Mqtt:ShutdownTpic`|MQTT Topic to watch|`bluewalk/shutdown`|
|`Config:Mqtt:ShutdownPayload`|MQTT Payload to match for shutdown|`yes`|
|`Config:Mqtt:ReportTopic`|MQTT Topic to report current status|`bluewalk/shutdown/report`|
|`Config:Esxi:Username`|ESXi username|null|
|`Config:Esxi:Password`|ESXi password|null|
|`Config:Esxi:Ip`|ESXi IP address|null|
|`Config:Esxi:Timeout`|ESXi shutdown VM's timeout, after passing will shutdown host|`300`|
|`Config:Esxi:VmToSkip`|ESXi VM's to skip during shutdown|null|
|`Config:TimeoutSeconds`|Timeout in seconds before initiating shutdown (grace period)|`300`|
|`Config:GpioPin`|GPIO Pin to monitor, when falling, the countdown is initated|`-1`|

Other environment variables regarding logging are available with the `Logging` prefix. For more information see [Microsoft Documentation](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-3.0#create-filter-rules-in-configuration)

__Example:__ To get Debug logs, add the following environment variable `Logging:Console:LogLevel:Net.Bluewalk=Debug`