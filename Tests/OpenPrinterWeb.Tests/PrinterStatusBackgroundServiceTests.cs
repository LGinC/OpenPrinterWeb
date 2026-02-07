using System;
using System.Threading;
using System.Threading.Tasks;
// using Microsoft.AspNetCore.SignalR; // Remove this
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using OpenPrinterWeb.Services;
// using OpenPrinterWeb.Hubs; // Remove this

namespace OpenPrinterWeb.Tests
{
    public class PrinterStatusBackgroundServiceTests
    {
        // private readonly Mock<IHubContext<PrinterHub>> _mockHubContext; // Remove this
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IPrintService> _mockPrintService;
        // private readonly Mock<IClientProxy> _mockClientProxy; // Remove this
        // private readonly Mock<IHubClients> _mockHubClients; // Remove this

        public PrinterStatusBackgroundServiceTests()
        {
            // _mockHubContext = new Mock<IHubContext<PrinterHub>>(); // Remove this
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockScope = new Mock<IServiceScope>();
            _mockPrintService = new Mock<IPrintService>();
            // _mockClientProxy = new Mock<IClientProxy>(); // Remove this
            // _mockHubClients = new Mock<IHubClients>(); // Remove this

            // Setup Service Provider Scope Chain
            _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(_mockScopeFactory.Object);
            
            _mockScopeFactory.Setup(x => x.CreateScope())
                .Returns(_mockScope.Object);
            
            _mockScope.Setup(x => x.ServiceProvider)
                .Returns(_mockServiceProvider.Object); 

            var scopedProvider = new Mock<IServiceProvider>();
            _mockScope.Setup(x => x.ServiceProvider).Returns(scopedProvider.Object);
            scopedProvider.Setup(x => x.GetService(typeof(IPrintService))).Returns(_mockPrintService.Object);

            // Setup Hub Context - Remove this
            // _mockHubContext.Setup(x => x.Clients).Returns(_mockHubClients.Object);
            // _mockHubClients.Setup(x => x.All).Returns(_mockClientProxy.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldFetchJobsAndNotifyClients()
        {
            // Arrange
            var jobs = new[] { new JobStatusInfo { Id = 1 } };
            _mockPrintService.Setup(x => x.GetJobsAsync()).ReturnsAsync(jobs);

            // Use 50ms for test to avoid tight loops
            var service = new PrinterStatusBackgroundService(_mockServiceProvider.Object, TimeSpan.FromMilliseconds(50));
            
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(500); // Allow enough time for at least one tick

            // Act
            try
            {
                await service.StartAsync(cts.Token);
                await Task.Delay(600); // Wait for cancellation
            }
            catch (TaskCanceledException)
            {
                // Expected
            }
            finally
            {
                await service.StopAsync(CancellationToken.None);
            }

            // Assert
            // Verify scope creation happened
            _mockScopeFactory.Verify(x => x.CreateScope(), Times.AtLeastOnce, "Scope was not created");
            
            // Verify service resolution
            // Note: GetRequiredService calls GetService.
            // scopedProvider verify? We can't easily verify the inner mock unless we kept a ref to it, 
            // but we can verify _mockPrintService.GetJobsAsync.
            
            _mockPrintService.Verify(x => x.GetJobsAsync(), Times.AtLeastOnce, "GetJobsAsync was not called");
            _mockPrintService.Verify(x => x.BroadcastJobUpdate(jobs), Times.AtLeastOnce, "BroadcastJobUpdate was not called");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleExceptions()
        {
            // Arrange
            _mockPrintService.Setup(x => x.GetJobsAsync()).ThrowsAsync(new Exception("failure"));

            var service = new PrinterStatusBackgroundService(_mockServiceProvider.Object, TimeSpan.FromMilliseconds(50));

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(500);

            // Act
            await service.StartAsync(cts.Token);
            await Task.Delay(600);
            await service.StopAsync(CancellationToken.None);

            // Assert
            _mockPrintService.Verify(x => x.GetJobsAsync(), Times.AtLeastOnce);
            _mockPrintService.Verify(x => x.BroadcastJobUpdate(It.IsAny<JobStatusInfo[]>()), Times.Never);
        }

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultPeriod()
        {
            // Arrange & Act
            var service = new PrinterStatusBackgroundService(_mockServiceProvider.Object);

            // Assert
            Assert.NotNull(service);
        }
    }
}
