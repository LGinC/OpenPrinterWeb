using System.IO;
using System.Threading.Tasks;

namespace OpenPrinterWeb.Services
{
    public interface IPdfConverter
    {
        Task<string> ConvertToPdfAsync(string inputFilePath);
    }
}
