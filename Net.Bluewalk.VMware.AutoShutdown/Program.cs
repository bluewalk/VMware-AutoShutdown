using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Bluewalk.VMware.AutoShutdown
{
    class Program
    {
        // AutoResetEvent to signal when to exit the application.
        private static readonly AutoResetEvent WaitHandle = new AutoResetEvent(false);
        
        static void Main(string[] args)
        {
            var version = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;
            Console.WriteLine($"VMware AutoShutdown version {version}");
            Console.WriteLine("https://github.com/bluewalk/bunq-ex\n");
            
            var logic = new Logic();
            
            // Fire and forget
            Task.Run(async () =>
            {
                await logic.Start();
                WaitHandle.WaitOne();
            });

            // Handle Control+C or Control+Break
            Console.CancelKeyPress += async (o, e) =>
            {
                Console.WriteLine("Exit");

                await logic.Stop();

                // Allow the manin thread to continue and exit...
                WaitHandle.Set();
            };

            // Wait
            WaitHandle.WaitOne();
        }
    }
}