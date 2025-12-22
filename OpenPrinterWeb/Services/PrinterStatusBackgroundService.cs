using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using OpenPrinterWeb.Hubs;
using OpenPrinterWeb.Services;

namespace OpenPrinterWeb.Services
{
    public class PrinterStatusBackgroundService : BackgroundService
    {
        private readonly IHubContext<PrinterHub> _hubContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(5));

        public PrinterStatusBackgroundService(IHubContext<PrinterHub> hubContext, IServiceProvider serviceProvider)
        {
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var printService = scope.ServiceProvider.GetRequiredService<IPrintService>();
                        var jobs = await printService.GetJobsAsync();

                        await _hubContext.Clients.All.SendAsync("JobUpdate", jobs, stoppingToken);
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
