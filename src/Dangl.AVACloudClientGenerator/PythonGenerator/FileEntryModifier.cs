using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.PythonGenerator
{
    public class FileEntryModifier
    {
        private readonly Stream _zipArchiveStream;

        public FileEntryModifier(Stream zipArchiveStream)
        {
            _zipArchiveStream = zipArchiveStream;
        }

        public async Task<Stream> FixDeserializationMethodAsync()
        {
            var memStream = new MemoryStream();
            await _zipArchiveStream.CopyToAsync(memStream);
            memStream.Position = 0;
            using (var archive = new ZipArchive(memStream, ZipArchiveMode.Update, true))
            {
                var apiClientEntry = archive.Entries.Single(e => e.FullName.EndsWith("api_client.py"));
                using (var entryStream = apiClientEntry.Open())
                {
                    using (var correctedEntryStream = await FixDeserializationInApiClientFile(entryStream))
                    {
                        apiClientEntry.Delete();
                        var updatedEntry = archive.CreateEntry(apiClientEntry.FullName);
                        using (var updatedEntrystream = updatedEntry.Open())
                        {
                            await correctedEntryStream.CopyToAsync(updatedEntrystream);
                        }
                    }
                }
            }
            memStream.Position = 0;
            return memStream;
        }

        private async Task<Stream> FixDeserializationInApiClientFile(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var pythonCode = await streamReader.ReadToEndAsync();

                pythonCode = Regex.Replace(pythonCode, "([ ]+)return self\\.__deserialize\\(data, response_type\\)",
                    "$1if response_type == 'ProjectDto':" + Environment.NewLine +
                    "$1    return data" + Environment.NewLine +
                    Environment.NewLine +
                    "$1return self.__deserialize(data, response_type)");

                var memStream = new MemoryStream();
                using (var streamWriter = new StreamWriter(memStream, Encoding.UTF8, 2048, true))
                {
                    await streamWriter.WriteAsync(pythonCode);
                }
                memStream.Position = 0;
                return memStream;
            }
        }
    }
}
