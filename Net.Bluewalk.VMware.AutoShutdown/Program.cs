using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Bluewalk.VMware.AutoShutdown.Models;

namespace Net.Bluewalk.VMware.AutoShutdown
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var version = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;
            Console.WriteLine($"VMware AutoShutdown version {version}");
            Console.WriteLine("https://github.com/bluewalk/VMware-AutoShutdown\n");


            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddEnvironmentVariables();

                    if (args != null)
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();

                    services.Replace(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(DateTimeLogger<>)));

                    services.Configure<Config>(hostContext.Configuration.GetSection("Config"));

                    services.AddSingleton<IHostedService, Logic>();
                })
                .ConfigureLogging((hostingContext, logging) => {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                });

            await builder.RunConsoleAsync();
        }
    }
}