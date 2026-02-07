using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Xunit;
using OpenPrinterWeb.Services;

namespace OpenPrinterWeb.Tests
{
    public class FileUploadServiceTests
    {
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly FileUploadService _service;

        public FileUploadServiceTests()
        {
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockFileSystem = new Mock<IFileSystem>();
            _service = new FileUploadService(_mockEnvironment.Object, _mockFileSystem.Object);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldCreateDirectory_WhenItDoesNotExist()
        {
            // Arrange
            var webRoot = "/app/wwwroot";
            var fileName = "test.pdf";
            var uploadPath = Path.Combine(webRoot, "uploads");
            
            _mockEnvironment.Setup(e => e.WebRootPath).Returns(webRoot);
            _mockFileSystem.Setup(fs => fs.DirectoryExists(uploadPath)).Returns(false);
            _mockFileSystem.Setup(fs => fs.CreateFile(It.IsAny<string>())).Returns(new MemoryStream());

            using var fileStream = new MemoryStream();

            // Act
            await _service.UploadFileAsync(fileStream, fileName);

            // Assert
            _mockFileSystem.Verify(fs => fs.CreateDirectory(uploadPath), Times.Once);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldNotCreateDirectory_WhenItExists()
        {
            // Arrange
            var webRoot = "/app/wwwroot";
            var fileName = "test.pdf";
            var uploadPath = Path.Combine(webRoot, "uploads");

            _mockEnvironment.Setup(e => e.WebRootPath).Returns(webRoot);
            _mockFileSystem.Setup(fs => fs.DirectoryExists(uploadPath)).Returns(true);
            _mockFileSystem.Setup(fs => fs.CreateFile(It.IsAny<string>())).Returns(new MemoryStream());

            using var fileStream = new MemoryStream();

            // Act
            await _service.UploadFileAsync(fileStream, fileName);

            // Assert
            _mockFileSystem.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldWriteToFile()
        {
            // Arrange
            var webRoot = "/app/wwwroot";
            var fileName = "test.txt";
            var fileContent = new byte[] { 1, 2, 3 };
            var outputStream = new MemoryStream();

            _mockEnvironment.Setup(e => e.WebRootPath).Returns(webRoot);
            _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
            _mockFileSystem.Setup(fs => fs.CreateFile(It.IsAny<string>())).Returns(outputStream);

            using var inputStream = new MemoryStream(fileContent);

            // Act
            var resultPath = await _service.UploadFileAsync(inputStream, fileName);

            // Assert
            Assert.Contains(fileName, resultPath);
            Assert.Contains(Path.Combine(webRoot, "uploads"), resultPath);
            Assert.Equal(fileContent.Length, outputStream.ToArray().Length);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldFallbackToContentRoot_WhenWebRootIsNull()
        {
            // Arrange
            var contentRoot = "/app";
            var fileName = "test.txt";
            var fileContent = new byte[] { 4, 5, 6 };
            var outputStream = new MemoryStream();
            var expectedUploadPath = Path.Combine(contentRoot, "wwwroot", "uploads");

            _mockEnvironment.Setup(e => e.WebRootPath).Returns((string?)null);
            _mockEnvironment.Setup(e => e.ContentRootPath).Returns(contentRoot);
            _mockFileSystem.Setup(fs => fs.DirectoryExists(expectedUploadPath)).Returns(true);
            _mockFileSystem.Setup(fs => fs.CreateFile(It.IsAny<string>())).Returns(outputStream);

            using var inputStream = new MemoryStream(fileContent);

            // Act
            var resultPath = await _service.UploadFileAsync(inputStream, fileName);

            // Assert
            Assert.Contains(Path.Combine(contentRoot, "wwwroot", "uploads"), resultPath);
            Assert.Equal(fileContent.Length, outputStream.ToArray().Length);
        }
    }
}
