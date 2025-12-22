using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenPrinterWeb.Services
{
    public interface IPrintService
    {
        Task<bool> PrintDocumentAsync(string jobName, Stream documentStream);
        Task<JobStatusInfo[]> GetJobsAsync();
        Task<PrinterInfo[]> GetPrintersAsync();
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
