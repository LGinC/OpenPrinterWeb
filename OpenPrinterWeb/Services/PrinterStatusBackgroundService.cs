using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection; // Ensure this is present
using OpenPrinterWeb.Services;

namespace OpenPrinterWeb.Services
{
    public class PrinterStatusBackgroundService : BackgroundService
    {
        // Removed IHubContext since we are using native C# events
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _period;
        private PeriodicTimer _timer;

        public PrinterStatusBackgroundService(IServiceProvider serviceProvider) 
            : this(serviceProvider, TimeSpan.FromSeconds(5))
        {
        }

        public PrinterStatusBackgroundService(IServiceProvider serviceProvider, TimeSpan period)
        {
            _serviceProvider = serviceProvider;
            _period = period;
            _timer = new PeriodicTimer(_period);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Use the singleton instance directly if possible, or resolve from scope if necessary.
                    // Since IPrintService is Singleton in Program.cs, we can resolve it once or per tick.
                    // Creating a scope is safer for dependencies.
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var printService = scope.ServiceProvider.GetRequiredService<IPrintService>();
                        var jobs = await printService.GetJobsAsync();

                        // Broadcast via C# event instead of SignalR
                        printService.BroadcastJobUpdate(jobs);
                    }
                }
                catch (Exception ex)
                {
                    // Log error
                    Console.WriteLine($"Error in background service: {ex.Message}");
                }
            }
        }
    }
}
