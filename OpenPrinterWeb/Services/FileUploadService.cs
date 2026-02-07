using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace OpenPrinterWeb.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IFileSystem _fileSystem;

        public FileUploadService(IWebHostEnvironment environment, IFileSystem fileSystem)
        {
            _environment = environment;
            _fileSystem = fileSystem;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
        {
            var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploadPath = Path.Combine(webRootPath, "uploads");
            if (!_fileSystem.DirectoryExists(uploadPath))
            {
                _fileSystem.CreateDirectory(uploadPath);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            using (var fileStreamOutput = _fileSystem.CreateFile(filePath))
            {
                await fileStream.CopyToAsync(fileStreamOutput);
            }

            return filePath;
        }
    }
}
