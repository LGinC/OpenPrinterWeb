using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OpenPrinterWeb.Services
{
    public class FileCleanupBackgroundService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileCleanupBackgroundService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IFileSystem _fileSystem;
        private readonly PeriodicTimer _timer;

        public FileCleanupBackgroundService(
            IConfiguration configuration, 
            ILogger<FileCleanupBackgroundService> logger,
            IWebHostEnvironment environment,
            IFileSystem fileSystem)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
            _fileSystem = fileSystem;
            _timer = new PeriodicTimer(TimeSpan.FromHours(24)); // Run once a day
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("File Cleanup Service is starting.");

            while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CleanupFiles();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during file cleanup.");
                }
            }
        }

        // Internal for testing
        public void CleanupFiles()
        {
            var retentionDays = _configuration.GetValue<int>("FileRetentionDays", 30);
            var uploadPath = Path.Combine(_environment.ContentRootPath, "data", "uploads");

            if (!_fileSystem.DirectoryExists(uploadPath))
            {
                _logger.LogWarning($"Upload directory not found at {uploadPath}");
                return;
            }

            var cutoffTime = DateTime.Now.AddDays(-retentionDays);
            var files = _fileSystem.GetFiles(uploadPath);
            var deletedCount = 0;

            foreach (var file in files)
            {
                // FileInfo creation time is hard to mock via IFileSystem unless we wrap FileInfo.
                // Updated IFileSystem to include GetCreationTime
                if (_fileSystem.GetCreationTime(file) < cutoffTime)
                {
                    try
                    {
                        _fileSystem.DeleteFile(file);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to delete file {file}");
                    }
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation($"Cleaned up {deletedCount} files older than {retentionDays} days.");
            }
        }
    }
}
