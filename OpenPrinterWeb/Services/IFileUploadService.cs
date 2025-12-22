using System.IO;
using System.Threading.Tasks;

namespace OpenPrinterWeb.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName);
    }
}
