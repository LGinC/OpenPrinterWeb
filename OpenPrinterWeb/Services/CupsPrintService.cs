using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SharpIpp;
using SharpIpp.Models;
using SharpIpp.Protocol.Models;


namespace OpenPrinterWeb.Services
{
    public class CupsPrintService : IPrintService
    {
        private readonly string _printerUri;
        private readonly SharpIppClient _client;

        public CupsPrintService(IConfiguration configuration)
        {
            _printerUri = configuration["PrinterSettings:Uri"] ?? "ipp://192.168.2.108:631/printers/default";
            _client = new SharpIppClient();
        }

        public async Task<bool> PrintDocumentAsync(string jobName, Stream documentStream)
        {
            try
            {
                var uri = new Uri(_printerUri);
                var request = new PrintJobRequest
                {
                    Document = documentStream,
                    OperationAttributes = new PrintJobOperationAttributes
                    {
                        PrinterUri = uri,
                        JobName = jobName
                    }
                };

                var response = await _client.PrintJobAsync(request);
                return response.JobState == JobState.Pending || response.JobState == JobState.Processing || response.JobState == JobState.PendingHeld;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error printing: {ex.Message}");
                // Log exception
                return false;
            }
        }

        public async Task<JobStatusInfo[]> GetJobsAsync()
        {
            try
            {
                var uri = new Uri(_printerUri);
                // GetJobsRequest usage
                var jobsRequest = new GetJobsRequest
                {
                    OperationAttributes = new GetJobsOperationAttributes
                    {
                        PrinterUri = uri,
                        WhichJobs = WhichJobs.NotCompleted
                    }
                };

                var response = await _client.GetJobsAsync(jobsRequest);
                
                return response.Jobs.Select(j => new JobStatusInfo
                {
                    Id = j.JobId.GetValueOrDefault(),
                    Name = j.JobName ?? "Unknown",
                    State = j.JobState.ToString() ?? "Unknown",
                    User = j.JobOriginatingUserName ?? "Unknown",
                    CreatedAt = DateTime.Now 
                }).ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting jobs: {ex.Message}");
                return Array.Empty<JobStatusInfo>();
            }
        }

        public async Task<PrinterInfo[]> GetPrintersAsync()
        {
            try
            {
                var baseUri = new Uri(_printerUri);
                var request = new CUPSGetPrintersRequest
                {
                    OperationAttributes = new CUPSGetPrintersOperationAttributes
                    {
                        PrinterUri = baseUri
                    }
                };
                
                var response = await _client.GetCUPSPrintersAsync(request);
                var printers = new List<PrinterInfo>();

                if (response.Sections != null)
                {
                    foreach (var section in response.Sections)
                    {
                        // Check if section is PrinterAttributes. 
                        // Tag might be an enum. We can try to match by value or assume PrinterAttributesTag (0x04)
                        if ((int)section.Tag == 4 /* PrinterAttributes */)
                        {
                            var info = new PrinterInfo();
                            foreach (var attr in section.Attributes)
                            {
                                switch (attr.Name)
                                {
                                    case "printer-name":
                                        info.Name = attr.Value?.ToString() ?? "Unknown";
                                        break;
                                    case "printer-uri-supported":
                                        // Usually a list of URIs, take first or string join
                                        if (attr.Value is IEnumerable<string> uris)
                                            info.Uri = uris.FirstOrDefault() ?? string.Empty;
                                        else if (attr.Value is object[] arr && arr.Length > 0)
                                            info.Uri = arr[0]?.ToString() ?? string.Empty;
                                        else 
                                            info.Uri = attr.Value?.ToString() ?? string.Empty;
                                        break;
                                    case "printer-info":
                                        info.Description = attr.Value?.ToString() ?? string.Empty;
                                        break;
                                    case "printer-state":
                                        // Map enum id to string if needed, or just stringify
                                        info.State = attr.Value?.ToString() ?? "Unknown";
                                        break;
                                }
                            }

                            // Determine if default
                            info.IsDefault = _printerUri.Contains(info.Name, StringComparison.OrdinalIgnoreCase);
                            
                            if (!string.IsNullOrEmpty(info.Name))
                            {
                                printers.Add(info);
                            }
                        }
                    }
                }

                return printers.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting printers: {ex.Message}");
                return Array.Empty<PrinterInfo>();
            }
        }
    }
}
