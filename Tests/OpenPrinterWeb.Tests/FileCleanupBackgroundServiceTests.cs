using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using OpenPrinterWeb.Services;

namespace OpenPrinterWeb.Tests
{
    public class FileCleanupBackgroundServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<FileCleanupBackgroundService>> _mockLogger;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly FileCleanupBackgroundService _service;

        public FileCleanupBackgroundServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<FileCleanupBackgroundService>>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockFileSystem = new Mock<IFileSystem>();

            _mockEnvironment.Setup(e => e.ContentRootPath).Returns("/app");
            // Mock Configuration Extension method GetValue is tricky, usually need to setup the section.
            // But since IConfiguration is a dictionary, setup standard GetSection or basic indexing.
            // GetValue<T> is an extension method that calls GetSection(key).Value.
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(s => s.Value).Returns("30");
            _mockConfig.Setup(c => c.GetSection("FileRetentionDays")).Returns(mockSection.Object);

            _service = new FileCleanupBackgroundService(
                _mockConfig.Object, 
                _mockLogger.Object, 
                _mockEnvironment.Object, 
                _mockFileSystem.Object);
        }

        [Fact]
        public void CleanupFiles_ShouldDeleteOldFiles()
        {
            // Arrange
            var uploadPath = Path.Combine("/app", "data", "uploads");
            var oldFile = Path.Combine(uploadPath, "old.pdf");
            var newFile = Path.Combine(uploadPath, "new.pdf");

            _mockFileSystem.Setup(fs => fs.DirectoryExists(uploadPath)).Returns(true);
            _mockFileSystem.Setup(fs => fs.GetFiles(uploadPath)).Returns(new[] { oldFile, newFile });
            
            // Old file: 31 days ago
            _mockFileSystem.Setup(fs => fs.GetCreationTime(oldFile)).Returns(DateTime.Now.AddDays(-31));
            // New file: 1 day ago
            _mockFileSystem.Setup(fs => fs.GetCreationTime(newFile)).Returns(DateTime.Now.AddDays(-1));

            // Act
            _service.CleanupFiles();

            // Assert
            _mockFileSystem.Verify(fs => fs.DeleteFile(oldFile), Times.Once);
            _mockFileSystem.Verify(fs => fs.DeleteFile(newFile), Times.Never);
        }

        [Fact]
        public void CleanupFiles_ShouldDoNothing_IfDirectoryMissing()
        {
            // Arrange
            var uploadPath = Path.Combine("/app", "data", "uploads");
            _mockFileSystem.Setup(fs => fs.DirectoryExists(uploadPath)).Returns(false);

            // Act
            _service.CleanupFiles();

            // Assert
            _mockFileSystem.Verify(fs => fs.GetFiles(It.IsAny<string>()), Times.Never);
        }
    }
}
