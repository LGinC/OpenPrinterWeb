using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OpenPrinterWeb.Services;
using Xunit;

namespace OpenPrinterWeb.Tests
{
    public class ProcessExecutorTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldCaptureOutputAndExitCode()
        {
            // Arrange
            var executor = new ProcessExecutor();
            var startInfo = CreateEchoProcessStartInfo("hello");

            // Act
            var result = await executor.ExecuteAsync(startInfo);

            // Assert
            Assert.Equal(0, result.ExitCode);
            Assert.Contains("hello", result.Output, StringComparison.OrdinalIgnoreCase);
        }

        private static ProcessStartInfo CreateEchoProcessStartInfo(string message)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new ProcessStartInfo("cmd", $"/c echo {message}");
            }

            return new ProcessStartInfo("/bin/sh", $"-c \"echo {message}\"");
        }
    }
}
