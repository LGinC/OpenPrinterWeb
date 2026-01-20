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
        private readonly SharpIppClient _client;

        public SharpIppClientAdapter()
        {
            _client = new SharpIppClient();
        }

        public Task<PrintJobResponse> PrintJobAsync(PrintJobRequest request) => _client.PrintJobAsync(request);
        public Task<GetJobsResponse> GetJobsAsync(GetJobsRequest request) => _client.GetJobsAsync(request);
        public Task<CUPSGetPrintersResponse> GetCUPSPrintersAsync(CUPSGetPrintersRequest request) => _client.GetCUPSPrintersAsync(request);
    }
}
