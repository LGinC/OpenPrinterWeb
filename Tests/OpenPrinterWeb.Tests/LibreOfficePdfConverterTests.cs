using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Moq;
using OpenPrinterWeb.Services;

namespace OpenPrinterWeb.Tests
{
    public class LibreOfficePdfConverterTests
    {
        private readonly Mock<IProcessExecutor> _mockProcessExecutor;
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly LibreOfficePdfConverter _converter;

        public LibreOfficePdfConverterTests()
        {
            _mockProcessExecutor = new Mock<IProcessExecutor>();
            _mockFileSystem = new Mock<IFileSystem>();
            _converter = new LibreOfficePdfConverter(_mockProcessExecutor.Object, _mockFileSystem.Object);
        }

        [Fact]
        public async Task ConvertToPdfAsync_ShouldReturnInputPath_WhenInputIsAlreadyPdf()
        {
            // Arrange
            var inputPath = "test.pdf";
            _mockFileSystem.Setup(fs => fs.Exists(inputPath)).Returns(true);

            // Act
            var result = await _converter.ConvertToPdfAsync(inputPath);

            // Assert
            Assert.Equal(inputPath, result);
            _mockProcessExecutor.Verify(p => p.ExecuteAsync(It.IsAny<ProcessStartInfo>()), Times.Never);
        }

        [Fact]
        public async Task ConvertToPdfAsync_ShouldThrowArgumentNullException_WhenInputIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _converter.ConvertToPdfAsync(null!));
        }

        [Fact]
        public async Task ConvertToPdfAsync_ShouldThrowFileNotFoundException_WhenInputFileDoesNotExist()
        {
            // Arrange
            var inputPath = "missing.docx";
            _mockFileSystem.Setup(fs => fs.Exists(inputPath)).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => _converter.ConvertToPdfAsync(inputPath));
        }

        [Fact]
        public async Task ConvertToPdfAsync_ShouldCallLibreOffice_WhenInputIsValid()
        {
            // Arrange
            var inputPath = Path.Combine("tmp", "test.docx");
            var outputPath = Path.Combine("tmp", "test.pdf");
            
            _mockFileSystem.Setup(fs => fs.Exists(inputPath)).Returns(true);
            _mockFileSystem.Setup(fs => fs.Exists(outputPath)).Returns(true); // Simulate success
            
            _mockProcessExecutor.Setup(p => p.ExecuteAsync(It.IsAny<ProcessStartInfo>()))
                .ReturnsAsync((0, "success", ""));

            // Act
            var result = await _converter.ConvertToPdfAsync(inputPath);

            // Assert
            Assert.Equal(outputPath, result);
            _mockProcessExecutor.Verify(p => p.ExecuteAsync(It.Is<ProcessStartInfo>(psi => 
                psi.FileName == "libreoffice" && 
                psi.Arguments.Contains(inputPath))), Times.Once);
        }

        [Fact]
        public async Task ConvertToPdfAsync_ShouldThrowException_WhenProcessFails()
        {
            // Arrange
            var inputPath = "test.docx";
            _mockFileSystem.Setup(fs => fs.Exists(inputPath)).Returns(true);
            
            _mockProcessExecutor.Setup(p => p.ExecuteAsync(It.IsAny<ProcessStartInfo>()))
                .ReturnsAsync((1, "", "Error converting"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _converter.ConvertToPdfAsync(inputPath));
            Assert.Contains("LibreOffice conversion failed", ex.Message);
        }
        
        [Fact]
        public async Task ConvertToPdfAsync_ShouldThrowFileNotFoundException_WhenProcessSucceedsButFileMissing()
        {
             // Arrange
            var inputPath = Path.Combine("tmp", "test.docx");
            var outputPath = Path.Combine("tmp", "test.pdf");
            
            _mockFileSystem.Setup(fs => fs.Exists(inputPath)).Returns(true);
            _mockFileSystem.Setup(fs => fs.Exists(outputPath)).Returns(false); // Simulate missing output
            
            _mockProcessExecutor.Setup(p => p.ExecuteAsync(It.IsAny<ProcessStartInfo>()))
                .ReturnsAsync((0, "success", ""));

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => _converter.ConvertToPdfAsync(inputPath));
        }
    }
}
