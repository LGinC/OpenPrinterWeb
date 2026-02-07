using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;

namespace OpenPrinterWeb.Services
{
    public class LibreOfficePdfConverter(IProcessExecutor processExecutor, IFileSystem fileSystem, IConfiguration configuration) : IPdfConverter
    {
        private readonly string? _windowsLibreOfficeBasePath = configuration["LibreOffice:WindowsBasePath"];

        public async Task<string> ConvertToPdfAsync(string inputFilePath)
        {
            if (string.IsNullOrEmpty(inputFilePath))
            {
                throw new ArgumentNullException(nameof(inputFilePath));
            }

            if (!fileSystem.Exists(inputFilePath))
            {
                throw new FileNotFoundException("Input file not found", inputFilePath);
            }

            var outputDir = Path.GetDirectoryName(inputFilePath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
            var expectedOutputPath = Path.Combine(outputDir!, fileNameWithoutExtension + ".pdf");

            // If it's already a PDF, just return it
            if (Path.GetExtension(inputFilePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return inputFilePath;
            }

            return await ConvertWithLibreOfficeAsync(inputFilePath, outputDir!, expectedOutputPath);
        }

        private async Task<string> ConvertWithLibreOfficeAsync(string inputFilePath, string outputDir, string expectedOutputPath)
        {
            var libreOfficeExecutable = ResolveLibreOfficeExecutable();
            var startInfo = new ProcessStartInfo
            {
                FileName = libreOfficeExecutable,
                Arguments = $"--headless --convert-to pdf --outdir \"{outputDir}\" \"{inputFilePath}\"",
                CreateNoWindow = true
            };

            var (exitCode, output, error) = await processExecutor.ExecuteAsync(startInfo);

            if (exitCode != 0)
            {
                throw new Exception($"LibreOffice conversion failed. ExitCode: {exitCode}. Error: {error}. Output: {output}");
            }

            return !fileSystem.Exists(expectedOutputPath) ? throw new FileNotFoundException($"Conversion finished but output file not found at {expectedOutputPath}") : expectedOutputPath;
        }

        private string ResolveLibreOfficeExecutable()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "libreoffice";
            }

            if (string.IsNullOrWhiteSpace(_windowsLibreOfficeBasePath))
            {
                throw new InvalidOperationException("LibreOffice WindowsBasePath is not configured. Set LibreOffice:WindowsBasePath in appsettings.json.");
            }

            var candidates = new[]
            {
                Path.Combine(_windowsLibreOfficeBasePath, "App", "libreoffice", "program", "soffice.exe"),
                Path.Combine(_windowsLibreOfficeBasePath, "program", "soffice.exe"),
                Path.Combine(_windowsLibreOfficeBasePath, "LibreOfficePortable.exe"),
                Path.Combine(_windowsLibreOfficeBasePath, "LibreOfficePortablePrevious.exe")
            };

            foreach (var candidate in candidates)
            {
                if (fileSystem.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new FileNotFoundException($"LibreOffice executable not found under {_windowsLibreOfficeBasePath}.");
        }
    }
}
