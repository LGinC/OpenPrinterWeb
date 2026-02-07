using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Moq;
using OpenPrinterWeb.Services;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace OpenPrinterWeb.Tests
{
    public class LibreOfficePdfConverterTests
    {
        private const string WindowsBasePath = "C:\\LibreOffice";
        private static readonly string LibreOfficeExecutable = Path.Combine(WindowsBasePath, "App", "libreoffice", "program", "soffice.exe");
        private readonly Mock<IProcessExecutor> _mockProcessExecutor;
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly LibreOfficePdfConverter _converter;

        public LibreOfficePdfConverterTests()
        {
            _mockProcessExecutor = new Mock<IProcessExecutor>();
            _mockFileSystem = new Mock<IFileSystem>();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["LibreOffice:WindowsBasePath"] = WindowsBasePath
                })
                .Build();
            _converter = new LibreOfficePdfConverter(_mockProcessExecutor.Object, _mockFileSystem.Object, configuration);
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
            SetupFileSystemExists(inputPath, outputPath: "missing.pdf", inputExists: false, outputExists: false);

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => _converter.ConvertToPdfAsync(inputPath));
        }

        [Fact]
        public async Task ConvertToPdfAsync_ShouldCallLibreOffice_WhenInputIsValid()
        {
            // Arrange
            var inputPath = Path.Combine("tmp", "test.docx");
            var outputPath = Path.Combine("tmp", "test.pdf");
            
            SetupFileSystemExists(inputPath, outputPath, inputExists: true, outputExists: true);
            
            _mockProcessExecutor.Setup(p => p.ExecuteAsync(It.IsAny<ProcessStartInfo>()))
                .ReturnsAsync((0, "success", ""));

            // Act
            var result = await _converter.ConvertToPdfAsync(inputPath);

            // Assert
            Assert.Equal(outputPath, result);
            _mockProcessExecutor.Verify(p => p.ExecuteAsync(It.Is<ProcessStartInfo>(psi => 
                psi.FileName == LibreOfficeExecutable && 
                psi.Arguments.Contains(inputPath))), Times.Once);
        }

        [Fact]
        public async Task ConvertToPdfAsync_ShouldThrowException_WhenProcessFails()
        {
            // Arrange
            var inputPath = Path.Combine("tmp", "test.docx");
            var outputPath = Path.Combine("tmp", "test.pdf");
            SetupFileSystemExists(inputPath, outputPath, inputExists: true, outputExists: false);
            
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
            
            SetupFileSystemExists(inputPath, outputPath, inputExists: true, outputExists: false);
            
            _mockProcessExecutor.Setup(p => p.ExecuteAsync(It.IsAny<ProcessStartInfo>()))
                .ReturnsAsync((0, "success", ""));

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => _converter.ConvertToPdfAsync(inputPath));
        }

        private void SetupFileSystemExists(string inputPath, string outputPath, bool inputExists, bool outputExists)
        {
            _mockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>()))
                .Returns<string>(path =>
                {
                    if (path == inputPath)
                    {
                        return inputExists;
                    }

                    if (path == outputPath)
                    {
                        return outputExists;
                    }

                    if (path == LibreOfficeExecutable)
                    {
                        return true;
                    }

                    return false;
                });
        }
    }
}
