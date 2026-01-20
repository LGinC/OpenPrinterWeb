using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace OpenPrinterWeb.Services
{
    public class LibreOfficePdfConverter : IPdfConverter
    {
        private readonly IProcessExecutor _processExecutor;
        private readonly IFileSystem _fileSystem;

        public LibreOfficePdfConverter(IProcessExecutor processExecutor, IFileSystem fileSystem)
        {
            _processExecutor = processExecutor;
            _fileSystem = fileSystem;
        }

        public async Task<string> ConvertToPdfAsync(string inputFilePath)
        {
            if (string.IsNullOrEmpty(inputFilePath))
            {
                throw new ArgumentNullException(nameof(inputFilePath));
            }

            if (!_fileSystem.Exists(inputFilePath))
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

            var startInfo = new ProcessStartInfo
            {
                FileName = "libreoffice",
                Arguments = $"--headless --convert-to pdf --outdir \"{outputDir}\" \"{inputFilePath}\"",
                CreateNoWindow = true
            };

            var (exitCode, output, error) = await _processExecutor.ExecuteAsync(startInfo);

            if (exitCode != 0)
            {
                throw new Exception($"LibreOffice conversion failed. ExitCode: {exitCode}. Error: {error}. Output: {output}");
            }

            if (!_fileSystem.Exists(expectedOutputPath))
            {
                 throw new FileNotFoundException($"Conversion finished but output file not found at {expectedOutputPath}");
            }

            return expectedOutputPath;
        }
    }
}
