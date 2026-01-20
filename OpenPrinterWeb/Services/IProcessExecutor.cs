using System.Diagnostics;
using System.Threading.Tasks;

namespace OpenPrinterWeb.Services
{
    public interface IProcessExecutor
    {
        Task<(int ExitCode, string Output, string Error)> ExecuteAsync(ProcessStartInfo startInfo);
    }

    public class ProcessExecutor : IProcessExecutor
    {
        public async Task<(int ExitCode, string Output, string Error)> ExecuteAsync(ProcessStartInfo startInfo)
        {
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            return (process.ExitCode, output, error);
        }
    }
}
