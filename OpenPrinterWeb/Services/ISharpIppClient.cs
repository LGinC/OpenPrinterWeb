using System;
using System.Threading;
using System.Threading.Tasks;
using SharpIpp;
using SharpIpp.Models;
using SharpIpp.Protocol.Models;

namespace OpenPrinterWeb.Services
{
    public interface ISharpIppClientWrapper
    {
        Task<PrintJobResponse> PrintJobAsync(PrintJobRequest request);
        Task<GetJobsResponse> GetJobsAsync(GetJobsRequest request);
        Task<CUPSGetPrintersResponse> GetCUPSPrintersAsync(CUPSGetPrintersRequest request);
    }

    public class SharpIppClientAdapter : ISharpIppClientWrapper
    {
        private readonly Func<PrintJobRequest, Task<PrintJobResponse>> _printJobAsync;
        private readonly Func<GetJobsRequest, Task<GetJobsResponse>> _getJobsAsync;
        private readonly Func<CUPSGetPrintersRequest, Task<CUPSGetPrintersResponse>> _getPrintersAsync;

        public SharpIppClientAdapter()
            : this(new SharpIppClient())
        {
        }

        internal SharpIppClientAdapter(SharpIppClient client)
            : this(
                request => client.PrintJobAsync(request, CancellationToken.None),
                request => client.GetJobsAsync(request, CancellationToken.None),
                request => client.GetCUPSPrintersAsync(request, CancellationToken.None))
        {
        }

        internal SharpIppClientAdapter(
            Func<PrintJobRequest, Task<PrintJobResponse>> printJobAsync,
            Func<GetJobsRequest, Task<GetJobsResponse>> getJobsAsync,
            Func<CUPSGetPrintersRequest, Task<CUPSGetPrintersResponse>> getPrintersAsync)
        {
            _printJobAsync = printJobAsync;
            _getJobsAsync = getJobsAsync;
            _getPrintersAsync = getPrintersAsync;
        }

        public Task<PrintJobResponse> PrintJobAsync(PrintJobRequest request) => _printJobAsync(request);
        public Task<GetJobsResponse> GetJobsAsync(GetJobsRequest request) => _getJobsAsync(request);

        public Task<CUPSGetPrintersResponse> GetCUPSPrintersAsync(CUPSGetPrintersRequest request) =>
            _getPrintersAsync(request);
    }
}
