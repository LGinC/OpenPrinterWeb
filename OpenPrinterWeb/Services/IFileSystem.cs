using System.IO;

namespace OpenPrinterWeb.Services
{
    public interface IFileSystem
    {
        bool Exists(string path);
        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        Stream CreateFile(string path);
        string[] GetFiles(string path);
        DateTime GetCreationTime(string path);
        void DeleteFile(string path);
    }

    public class PhysicalFileSystem : IFileSystem
    {
        public bool Exists(string path) => File.Exists(path);
        public bool DirectoryExists(string path) => Directory.Exists(path);
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);
        public Stream CreateFile(string path) => new FileStream(path, FileMode.Create);
        public string[] GetFiles(string path) => Directory.GetFiles(path);
        public DateTime GetCreationTime(string path) => File.GetCreationTime(path);
        public void DeleteFile(string path) => File.Delete(path);
    }
}
