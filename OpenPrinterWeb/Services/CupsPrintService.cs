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
        private readonly ISharpIppClientWrapper _client;

        public CupsPrintService(IConfiguration configuration, ISharpIppClientWrapper client)
        {
            _printerUri = configuration["PrinterSettings:Uri"] ?? "ipp://192.168.2.108:631/printers/default";
            _client = client;
        }

        public async Task<bool> PrintDocumentAsync(string jobName, Stream documentStream, string? printerUri = null, PrintOptions? options = null)
        {
            try
            {
                var targetUri = printerUri ?? _printerUri;
                var uri = new Uri(targetUri);
                // Add color mode attributes explicitly for better compatibility with CUPS
                var additionalAttributes = new List<IppAttribute>();
                if (options?.ColorMode == OpenPrinterWeb.Services.PrintColorMode.Monochrome)
                {
                    additionalAttributes.Add(new IppAttribute(Tag.Keyword, "print-color-mode", "monochrome"));
                    additionalAttributes.Add(new IppAttribute(Tag.NameWithoutLanguage, "ColorModel", "Gray"));
                }
                else
                {
                    additionalAttributes.Add(new IppAttribute(Tag.Keyword, "print-color-mode", "color"));
                    additionalAttributes.Add(new IppAttribute(Tag.NameWithoutLanguage, "ColorModel", "RGB"));
                }

                var request = new PrintJobRequest
                {
                    Document = documentStream,
                    OperationAttributes = new PrintJobOperationAttributes
                    {
                        PrinterUri = uri,
                        JobName = jobName
                    },
                    JobTemplateAttributes = new JobTemplateAttributes
                    {
                        Copies = options?.Copies ?? 1,
                        OrientationRequested = options?.Orientation == PrintOrientation.Landscape
                            ? Orientation.Landscape
                            : Orientation.Portrait,
                        PrintColorMode = options?.ColorMode == OpenPrinterWeb.Services.PrintColorMode.Monochrome
                            ? SharpIpp.Protocol.Models.PrintColorMode.Monochrome
                            : SharpIpp.Protocol.Models.PrintColorMode.Color
                    },
                    AdditionalJobAttributes = additionalAttributes
                };

                if (options != null && !string.IsNullOrEmpty(options.PageRange))
                {
                    // Parse "1-5, 8" etc.
                    var ranges = new List<SharpIpp.Protocol.Models.Range>();
                    var parts = options.PageRange.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        var rangeParts = part.Trim().Split('-', StringSplitOptions.RemoveEmptyEntries);
                        if (rangeParts.Length == 1 && int.TryParse(rangeParts[0], out var single))
                        {
                            ranges.Add(new SharpIpp.Protocol.Models.Range(single, single));
                        }
                        else if (rangeParts.Length == 2 && int.TryParse(rangeParts[0], out var low) && int.TryParse(rangeParts[1], out var high))
                        {
                            ranges.Add(new SharpIpp.Protocol.Models.Range(low, high));
                        }
                    }
                    if (ranges.Any())
                    {
                        request.JobTemplateAttributes.PageRanges = ranges.ToArray();
                    }
                }

                var response = await _client.PrintJobAsync(request);
                return response.JobState == JobState.Pending || response.JobState == JobState.Processing || response.JobState == JobState.PendingHeld;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error printing: {ex}");
                // Log exception
                return false;
            }
        }

        public async Task<JobStatusInfo[]> GetJobsAsync()
        {
            try
            {
                // Use base server URI for getting all jobs
                var uri = new Uri(_printerUri);
                var baseUri = new Uri($"{uri.Scheme}://{uri.Host}:{uri.Port}");

                var jobsRequest = new GetJobsRequest
                {
                    OperationAttributes = new GetJobsOperationAttributes
                    {
                        PrinterUri = baseUri,
                        WhichJobs = WhichJobs.NotCompleted
                    }
                };

                Console.WriteLine($"DEBUG: Fetching jobs from {baseUri}");
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
                Console.WriteLine($"DEBUG: Error getting jobs: {ex}");
                return Array.Empty<JobStatusInfo>();
            }
        }

        public async Task<PrinterInfo[]> GetPrintersAsync()
        {
            try
            {
                var uri = new Uri(_printerUri);
                var baseUri = new Uri($"{uri.Scheme}://{uri.Host}:{uri.Port}");

                var request = new CUPSGetPrintersRequest
                {
                    OperationAttributes = new CUPSGetPrintersOperationAttributes
                    {
                        PrinterUri = baseUri
                    }
                };

                Console.WriteLine($"DEBUG: Fetching printers from {baseUri}");
                var response = await _client.GetCUPSPrintersAsync(request);
                var printers = new List<PrinterInfo>();

                if (response.Sections != null)
                {
                    foreach (var section in response.Sections)
                    {
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
                Console.WriteLine($"DEBUG: Error getting printers: {ex}");
                return Array.Empty<PrinterInfo>();
            }
        }
    }
}
