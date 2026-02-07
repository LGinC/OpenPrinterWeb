using System;
using System.IO;
using OpenPrinterWeb.Services;
using Xunit;

namespace OpenPrinterWeb.Tests
{
    public class PhysicalFileSystemTests
    {
        [Fact]
        public void PhysicalFileSystem_ShouldPerformFileOperations()
        {
            var fileSystem = new PhysicalFileSystem();
            var root = Path.Combine(Path.GetTempPath(), "OpenPrinterWebTests", Guid.NewGuid().ToString("N"));
            var filePath = Path.Combine(root, "test.txt");

            try
            {
                fileSystem.CreateDirectory(root);

                using (var stream = fileSystem.CreateFile(filePath))
                {
                    var data = new byte[] { 1, 2, 3 };
                    stream.Write(data, 0, data.Length);
                }

                Assert.True(fileSystem.DirectoryExists(root));
                Assert.True(fileSystem.Exists(filePath));
                Assert.Single(fileSystem.GetFiles(root));
                Assert.True(fileSystem.GetCreationTime(filePath) <= DateTime.Now);

                fileSystem.DeleteFile(filePath);
                Assert.False(fileSystem.Exists(filePath));
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }
    }
}
