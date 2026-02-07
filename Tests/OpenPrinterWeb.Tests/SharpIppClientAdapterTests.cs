using System.Threading.Tasks;
using OpenPrinterWeb.Services;
using SharpIpp.Models;
using SharpIpp.Protocol.Models;
using Xunit;

namespace OpenPrinterWeb.Tests
{
    public class SharpIppClientAdapterTests
    {
        [Fact]
        public async Task SharpIppClientAdapter_ShouldForwardCalls()
        {
            // Arrange
            var printCalled = false;
            var getJobsCalled = false;
            var getPrintersCalled = false;

            var adapter = new SharpIppClientAdapter(
                request =>
                {
                    printCalled = true;
                    return Task.FromResult(new PrintJobResponse { JobState = JobState.Pending });
                },
                request =>
                {
                    getJobsCalled = true;
                    return Task.FromResult(new GetJobsResponse());
                },
                request =>
                {
                    getPrintersCalled = true;
                    return Task.FromResult(new CUPSGetPrintersResponse());
                });

            // Act
            await adapter.PrintJobAsync(new PrintJobRequest());
            await adapter.GetJobsAsync(new GetJobsRequest());
            await adapter.GetCUPSPrintersAsync(new CUPSGetPrintersRequest());

            // Assert
            Assert.True(printCalled);
            Assert.True(getJobsCalled);
            Assert.True(getPrintersCalled);
        }
    }
}
