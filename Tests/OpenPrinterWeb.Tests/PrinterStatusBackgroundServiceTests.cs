using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using OpenPrinterWeb.Services;
using OpenPrinterWeb.Hubs;

namespace OpenPrinterWeb.Tests
{
    public class PrinterStatusBackgroundServiceTests
    {
        private readonly Mock<IHubContext<PrinterHub>> _mockHubContext;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IPrintService> _mockPrintService;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<IHubClients> _mockHubClients;

        public PrinterStatusBackgroundServiceTests()
        {
            _mockHubContext = new Mock<IHubContext<PrinterHub>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockScope = new Mock<IServiceScope>();
            _mockPrintService = new Mock<IPrintService>();
            _mockClientProxy = new Mock<IClientProxy>();
            _mockHubClients = new Mock<IHubClients>();

            // Setup Service Provider Scope Chain
            _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(_mockScopeFactory.Object);
            
            _mockScopeFactory.Setup(x => x.CreateScope())
                .Returns(_mockScope.Object);
            
            _mockScope.Setup(x => x.ServiceProvider)
                .Returns(_mockServiceProvider.Object); // Simplified: ServiceProvider returns itself for resolution in test context? No, safer to separate.

            // Use a separate mock provider for the scope to avoid circular loops or confusion, but reusing _mockServiceProvider is fine if careful.
            // Let's create a dedicated provider for the scope
            var scopedProvider = new Mock<IServiceProvider>();
            _mockScope.Setup(x => x.ServiceProvider).Returns(scopedProvider.Object);
            scopedProvider.Setup(x => x.GetService(typeof(IPrintService))).Returns(_mockPrintService.Object);

            // Setup Hub Context
            _mockHubContext.Setup(x => x.Clients).Returns(_mockHubClients.Object);
            _mockHubClients.Setup(x => x.All).Returns(_mockClientProxy.Object);
        }

        // Note: Testing BackgroundService.ExecuteAsync directly is hard because it runs in a loop.
        // We can test the logic if we extract it, or we can just skip testing the loop itself and assume framework reliability.
        // However, a common trick is to cancel the token.
        
        [Fact]
        public async Task ExecuteAsync_ShouldFetchJobsAndNotifyClients()
        {
            // Arrange
            var jobs = new[] { new JobStatusInfo { Id = 1 } };
            _mockPrintService.Setup(x => x.GetJobsAsync()).ReturnsAsync(jobs);

            // Use 10ms for test
            var service = new PrinterStatusBackgroundService(_mockHubContext.Object, _mockServiceProvider.Object, TimeSpan.FromMilliseconds(10));
            
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(50); // Allow enough time for at least one tick

            // Act
            try
            {
                await service.StartAsync(cts.Token);
                await Task.Delay(60); // Wait for cancellation
            }
            catch (TaskCanceledException)
            {
                // Expected
            }

            // Assert
            _mockPrintService.Verify(x => x.GetJobsAsync(), Times.AtLeastOnce);
            _mockClientProxy.Verify(x => x.SendCoreAsync("JobUpdate", It.Is<object[]>(o => o != null && o.Length > 0), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }
    }
}
