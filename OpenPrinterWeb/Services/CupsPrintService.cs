using SharpIpp.Models;
using SharpIpp.Protocol.Models;


namespace OpenPrinterWeb.Services
{
    public partial class CupsPrintService(
        IConfiguration configuration,
        ISharpIppClientWrapper client,
        ILogger<CupsPrintService> logger)
        : IPrintService
    {
        private readonly string _printerUri =
            configuration["PrinterSettings:Uri"] ?? throw new ArgumentNullException("PrinterSettings:Uri");

        public event Action<JobStatusInfo[]>? OnJobUpdate;

        public void BroadcastJobUpdate(JobStatusInfo[] jobs)
        {
            OnJobUpdate?.Invoke(jobs);
        }

        public async Task<bool> PrintDocumentAsync(string jobName, Stream documentStream, string? printerUri = null,
            PrintOptions? options = null)
        {
            try
            {
                var targetUri = printerUri ?? _printerUri;
                var uri = new Uri(targetUri);
                // Add color mode attributes explicitly for better compatibility with CUPS
                var additionalAttributes = new List<IppAttribute>();
                if (options?.ColorMode == PrintColorMode.Monochrome)
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
                        else if (rangeParts.Length == 2 && int.TryParse(rangeParts[0], out var low) &&
                                 int.TryParse(rangeParts[1], out var high))
                        {
                            ranges.Add(new SharpIpp.Protocol.Models.Range(low, high));
                        }
                    }

                    if (ranges.Any())
                    {
                        request.JobTemplateAttributes.PageRanges = ranges.ToArray();
                    }
                }

                var response = await client.PrintJobAsync(request);
                return response.JobState == JobState.Pending || response.JobState == JobState.Processing ||
                       response.JobState == JobState.PendingHeld;
            }
            catch (Exception ex)
            {
                LogErrorPrinting(logger, ex);
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

                var response = await client.GetJobsAsync(jobsRequest);
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
                LogErrorGettingJobs(logger, ex);
                return [];
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

                var response = await client.GetCUPSPrintersAsync(request);
                var printers = new List<PrinterInfo>();

                foreach (var section in response.Sections)
                {
                    if ((int)section.Tag != 4 /* PrinterAttributes */) continue;
                    var info = new PrinterInfo();
                    foreach (var attr in section.Attributes)
                    {
                        switch (attr.Name)
                        {
                            case "printer-name":
                                info.Name = attr.Value?.ToString() ?? "Unknown";
                                break;
                            case "printer-uri-supported":
                                info.Uri = attr.Value switch
                                {
                                    // Usually a list of URIs, take first or string join
                                    IEnumerable<string> uris => uris.FirstOrDefault() ?? string.Empty,
                                    object[] { Length: > 0 } arr => arr[0]?.ToString() ?? string.Empty,
                                    _ => attr.Value?.ToString() ?? string.Empty
                                };
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

                return printers.ToArray();
            }
            catch (Exception ex)
            {
                LogErrorGettingPrinters(logger, ex);
                return [];
            }
        }

        [LoggerMessage(LogLevel.Error, "Error printing")]
        static partial void LogErrorPrinting(ILogger<CupsPrintService> logger, Exception exception);

        [LoggerMessage(LogLevel.Error, "Error getting jobs")]
        static partial void LogErrorGettingJobs(ILogger<CupsPrintService> logger, Exception ex);

        [LoggerMessage(LogLevel.Error, "Error getting printers")]
        static partial void LogErrorGettingPrinters(ILogger<CupsPrintService> logger, Exception ex);
    }
}