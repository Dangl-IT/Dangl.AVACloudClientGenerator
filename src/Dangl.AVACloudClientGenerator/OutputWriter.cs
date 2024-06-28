using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator
{
    public class OutputWriter
    {
        private readonly Stream _generatedCodeZipArchiveStream;
        private readonly string _outputPathFolder;

        public OutputWriter(Stream generatedCodeZipArchiveStream,
            string outputPathFolder)
        {
            _generatedCodeZipArchiveStream = generatedCodeZipArchiveStream;
            _outputPathFolder = outputPathFolder;
        }

        public async Task WriteCodeToDirectoryAndAddReadmeAndLicense(bool shouldAddReadme)
        {
            using (var zipArchive = new System.IO.Compression.ZipArchive(_generatedCodeZipArchiveStream))
            {
                foreach (var entry in zipArchive.Entries)
                {
                    await WriteSingleEntryAsync(entry);
                }
            }

            if (shouldAddReadme)
            {
                var readmeText = ReadmeFactory.GetReadmeContent(packageDetails: null);
                await WriteTextFile("README.md", readmeText);
            }
            var licenseText = LicenseFactory.GetLicenseContent();
            await WriteTextFile("LICENSE.md", licenseText);
        }

        private async Task WriteTextFile(string relativePath, string content)
        {
            using (var fs = File.Create(GetFilePath(relativePath)))
            {
                using (var sw = new StreamWriter(fs))
                {
                    await sw.WriteAsync(content);
                }
            }
        }

        private async Task WriteSingleEntryAsync(ZipArchiveEntry entry)
        {
            var filePath = GetFilePath(entry);
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            using (var entryStream = entry.Open())
            {
                using (var outputStream = File.Create(filePath))
                {
                    await entryStream.CopyToAsync(outputStream);
                }
            }
        }

        private string GetFilePath(string relativePath)
        {
            return Path.Combine(_outputPathFolder, relativePath);
        }

        private string GetFilePath(ZipArchiveEntry entry)
        {
            return Path.Combine(_outputPathFolder, entry.FullName);
        }
    }
}
