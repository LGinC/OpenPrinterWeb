using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenPrinterWeb.Services
{
    public interface IPrintService
    {
        event Action<JobStatusInfo[]>? OnJobUpdate;
        void BroadcastJobUpdate(JobStatusInfo[] jobs);

        Task<bool> PrintDocumentAsync(string jobName, Stream documentStream, string? printerUri = null, PrintOptions? options = null);
        Task<JobStatusInfo[]> GetJobsAsync();
        Task<PrinterInfo[]> GetPrintersAsync();
    }

    public enum PrintOrientation
    {
        Portrait,
        Landscape
    }

    public enum PrintColorMode
    {
        Color,
        Monochrome
    }

    public class PrintOptions
    {
        public PrintColorMode ColorMode { get; set; } = PrintColorMode.Color;
        public int Copies { get; set; } = 1;
        public string? PageRange { get; set; }
        public PrintOrientation Orientation { get; set; } = PrintOrientation.Portrait;
    }

    public class JobStatusInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PrinterInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Uri { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
