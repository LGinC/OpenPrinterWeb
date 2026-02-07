using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // Add this
using Moq;
using Xunit;
using OpenPrinterWeb.Services;
using SharpIpp;
using SharpIpp.Models;
using SharpIpp.Protocol.Models;

namespace OpenPrinterWeb.Tests
{
    public class CupsPrintServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ISharpIppClientWrapper> _mockClient;
        private readonly Mock<ILogger<CupsPrintService>> _mockLogger; // Add this
        private readonly CupsPrintService _service;

        public CupsPrintServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockClient = new Mock<ISharpIppClientWrapper>();
            _mockLogger = new Mock<ILogger<CupsPrintService>>(); // Add this
            
            _mockConfig.Setup(c => c["PrinterSettings:Uri"]).Returns("ipp://localhost:631/printers/OfficePrinter");

            _service = new CupsPrintService(_mockConfig.Object, _mockClient.Object, _mockLogger.Object); // Update this
        }

        [Fact]
        public async Task PrintDocumentAsync_ShouldReturnTrue_WhenPrintSucceeds()
        {
            // Arrange
            var jobName = "Test Job";
            var stream = new MemoryStream();
            
            _mockClient.Setup(c => c.PrintJobAsync(It.IsAny<PrintJobRequest>()))
                .ReturnsAsync(new PrintJobResponse { JobState = JobState.Pending });

            // Act
            var result = await _service.PrintDocumentAsync(jobName, stream);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task PrintDocumentAsync_ShouldReturnFalse_WhenExceptionOccurs()
        {
            // Arrange
            var jobName = "Test Job";
            var stream = new MemoryStream();

            _mockClient.Setup(c => c.PrintJobAsync(It.IsAny<PrintJobRequest>()))
                .ThrowsAsync(new Exception("Print failed"));

            // Act
            var result = await _service.PrintDocumentAsync(jobName, stream);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task PrintDocumentAsync_ShouldIncludeMonochromeAttributesAndPageRanges_WhenProvided()
        {
            // Arrange
            var jobName = "Test Job";
            var stream = new MemoryStream();
            PrintJobRequest? capturedRequest = null;

            _mockClient.Setup(c => c.PrintJobAsync(It.IsAny<PrintJobRequest>()))
                .Callback<PrintJobRequest>(request => capturedRequest = request)
                .ReturnsAsync(new PrintJobResponse { JobState = JobState.Processing });

            var options = new PrintOptions
            {
                Copies = 2,
                Orientation = PrintOrientation.Landscape,
                ColorMode = Services.PrintColorMode.Monochrome,
                PageRange = "1-3, 5"
            };

            // Act
            var result = await _service.PrintDocumentAsync(jobName, stream, options: options);

            // Assert
            Assert.True(result);
            Assert.NotNull(capturedRequest);
            Assert.Equal(2, capturedRequest!.JobTemplateAttributes.Copies);
            Assert.Equal(Orientation.Landscape, capturedRequest.JobTemplateAttributes.OrientationRequested);
            Assert.Equal(SharpIpp.Protocol.Models.PrintColorMode.Monochrome, capturedRequest.JobTemplateAttributes.PrintColorMode);
            Assert.NotNull(capturedRequest.JobTemplateAttributes.PageRanges);
            Assert.Contains(capturedRequest.AdditionalJobAttributes,
                attribute => attribute.Name == "print-color-mode" && attribute.Value?.ToString() == "monochrome");
        }

        [Fact]
        public async Task PrintDocumentAsync_ShouldUseColorAttributes_WhenOptionsAreNull()
        {
            // Arrange
            var jobName = "Test Job";
            var stream = new MemoryStream();
            PrintJobRequest? capturedRequest = null;

            _mockClient.Setup(c => c.PrintJobAsync(It.IsAny<PrintJobRequest>()))
                .Callback<PrintJobRequest>(request => capturedRequest = request)
                .ReturnsAsync(new PrintJobResponse { JobState = JobState.Processing });

            // Act
            var result = await _service.PrintDocumentAsync(jobName, stream, options: null);

            // Assert
            Assert.True(result);
            Assert.NotNull(capturedRequest);
            Assert.Contains(capturedRequest!.AdditionalJobAttributes,
                attribute => attribute.Name == "print-color-mode" && attribute.Value?.ToString() == "color");
        }

        [Fact]
        public async Task PrintDocumentAsync_ShouldNotSetPageRanges_WhenRangeIsInvalid()
        {
            // Arrange
            var jobName = "Test Job";
            var stream = new MemoryStream();
            PrintJobRequest? capturedRequest = null;

            _mockClient.Setup(c => c.PrintJobAsync(It.IsAny<PrintJobRequest>()))
                .Callback<PrintJobRequest>(request => capturedRequest = request)
                .ReturnsAsync(new PrintJobResponse { JobState = JobState.Pending });

            var options = new PrintOptions
            {
                PageRange = "invalid"
            };

            // Act
            var result = await _service.PrintDocumentAsync(jobName, stream, options: options);

            // Assert
            Assert.True(result);
            Assert.NotNull(capturedRequest);
            Assert.Null(capturedRequest!.JobTemplateAttributes.PageRanges);
        }

        [Fact]
        public async Task PrintDocumentAsync_ShouldReturnFalse_WhenJobStateIsNotPendingOrProcessing()
        {
            // Arrange
            var jobName = "Test Job";
            var stream = new MemoryStream();

            _mockClient.Setup(c => c.PrintJobAsync(It.IsAny<PrintJobRequest>()))
                .ReturnsAsync(new PrintJobResponse { JobState = JobState.Completed });

            // Act
            var result = await _service.PrintDocumentAsync(jobName, stream);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetJobsAsync_ShouldReturnJobs_WhenClientReturnsJobs()
        {
            // Arrange
            var jobs = new[]
            {
                new JobDescriptionAttributes
                {
                    JobId = 1,
                    JobName = "Job 1",
                    JobState = JobState.Processing,
                    JobOriginatingUserName = "User"
                },
                new JobDescriptionAttributes()
            };

            _mockClient.Setup(c => c.GetJobsAsync(It.IsAny<GetJobsRequest>()))
                .ReturnsAsync(new GetJobsResponse { Jobs = jobs });

            // Act
            var result = await _service.GetJobsAsync();

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal(1, result[0].Id);
            Assert.Equal("Job 1", result[0].Name);
            Assert.Equal("User", result[0].User);
            Assert.Equal("Processing", result[0].State);
            Assert.Equal("Unknown", result[1].Name);
            Assert.Equal("Unknown", result[1].User);
        }

        [Fact]
        public async Task GetJobsAsync_ShouldReturnEmpty_WhenExceptionOccurs()
        {
            // Arrange
            _mockClient.Setup(c => c.GetJobsAsync(It.IsAny<GetJobsRequest>()))
                .ThrowsAsync(new Exception("Jobs failed"));

            // Act
            var result = await _service.GetJobsAsync();

            // Assert
            Assert.Empty(result);
        }

        /*
        [Fact]
        public async Task GetJobsAsync_ShouldReturnJobs_WhenClientReturnsJobs()
        {
            // Arrange
            // Using correct JobAttributes type if possible, or assume namespaces are tricky.
            // SharpIpp has different models. Let's try to infer from previous usage.
            // Looking at CupsPrintService.cs: response.Jobs is used.
            // It seems response.Jobs is a collection of JobAttributes.
            
            var jobList = new System.Collections.Generic.List<SharpIpp.Protocol.Models.JobAttributes> 
            { 
                new SharpIpp.Protocol.Models.JobAttributes { JobId = 1, JobName = "Job 1", JobState = JobState.Completed, JobOriginatingUserName = "User" } 
            };

            _mockClient.Setup(c => c.GetJobsAsync(It.IsAny<GetJobsRequest>()))
                .ReturnsAsync(new GetJobsResponse { Jobs = jobList });

            // Act
            var result = await _service.GetJobsAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
            Assert.Equal("Job 1", result[0].Name);
        }
        */

        [Fact]
        public async Task GetPrintersAsync_ShouldReturnPrinters()
        {
            // Arrange
            var response = new CUPSGetPrintersResponse();
            var sections = new System.Collections.Generic.List<IppSection>
            {
                CreatePrinterSection(
                    name: "OfficePrinter",
                    uriValue: new[] { "ipp://localhost:631/printers/OfficePrinter" },
                    description: "Office Printer",
                    state: "Idle"),
                CreatePrinterSection(
                    name: "LabPrinter",
                    uriValue: new object[] { "ipp://localhost:631/printers/LabPrinter" },
                    description: "Lab Printer",
                    state: "Busy"),
                CreatePrinterSection(
                    name: "",
                    uriValue: "ipp://localhost:631/printers/Hidden",
                    description: "Hidden Printer",
                    state: "Unknown")
            };

            SetBackingField(response, "<Sections>k__BackingField", sections);
            
            _mockClient.Setup(c => c.GetCUPSPrintersAsync(It.IsAny<CUPSGetPrintersRequest>()))
                .ReturnsAsync(response);

            // Act
            var result = await _service.GetPrintersAsync();

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal("OfficePrinter", result[0].Name);
            Assert.Equal("ipp://localhost:631/printers/OfficePrinter", result[0].Uri);
            Assert.Equal("Office Printer", result[0].Description);
            Assert.Equal("Idle", result[0].State);
            Assert.True(result[0].IsDefault);
            Assert.Equal("LabPrinter", result[1].Name);
            Assert.Equal("ipp://localhost:631/printers/LabPrinter", result[1].Uri);
            Assert.Equal("Lab Printer", result[1].Description);
            Assert.Equal("Busy", result[1].State);
            Assert.False(result[1].IsDefault);
        }

        [Fact]
        public async Task GetPrintersAsync_ShouldReturnEmpty_WhenExceptionOccurs()
        {
            // Arrange
            _mockClient.Setup(c => c.GetCUPSPrintersAsync(It.IsAny<CUPSGetPrintersRequest>()))
                .ThrowsAsync(new Exception("Printers failed"));

            // Act
            var result = await _service.GetPrintersAsync();

            // Assert
            Assert.Empty(result);
        }

        private static IppSection CreatePrinterSection(string name, object uriValue, string description, string state)
        {
            var section = new IppSection
            {
                Tag = SectionTag.PrinterAttributesTag
            };

            var attributes = new System.Collections.Generic.List<IppAttribute>
            {
                CreateAttribute(Tag.NameWithoutLanguage, "printer-name", name),
                CreateAttribute(Tag.Uri, "printer-uri-supported", uriValue),
                CreateAttribute(Tag.TextWithoutLanguage, "printer-info", description),
                CreateAttribute(Tag.Keyword, "printer-state", state)
            };

            SetBackingField(section, "<Attributes>k__BackingField", attributes);
            return section;
        }

        private static IppAttribute CreateAttribute(Tag tag, string name, object value)
        {
            var attribute = new IppAttribute();
            SetBackingField(attribute, "<Tag>k__BackingField", tag);
            SetBackingField(attribute, "<Name>k__BackingField", name);
            SetBackingField(attribute, "<Value>k__BackingField", value);
            return attribute;
        }

        private static void SetBackingField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
        {
            var field = typeof(TTarget).GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field!.SetValue(target, value);
        }
    }
}
