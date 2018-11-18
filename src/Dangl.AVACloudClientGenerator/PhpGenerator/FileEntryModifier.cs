using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.PhpGenerator
{
    public class FileEntryModifier
    {
        private readonly Stream _zipArchiveStream;

        public FileEntryModifier(Stream zipArchiveStream)
        {
            _zipArchiveStream = zipArchiveStream;
        }

        public async Task<Stream> UpdateComposerJsonAsync()
        {
            var memStream = new MemoryStream();
            await _zipArchiveStream.CopyToAsync(memStream);
            memStream.Position = 0;
            using (var archive = new ZipArchive(memStream, ZipArchiveMode.Update, true))
            {
                var composerJsonEntry = archive.Entries.Single(e => e.FullName.EndsWith("composer.json"));
                using (var entryStream = composerJsonEntry.Open())
                {
                    using (var correctedStream = await UpdateComposerJsonAsync(entryStream))
                    {
                        composerJsonEntry.Delete();
                        var updateEntry = archive.CreateEntry(composerJsonEntry.FullName);
                        using (var updatedEntryStream = updateEntry.Open())
                        {
                            await correctedStream.CopyToAsync(updatedEntryStream);
                        }
                    }
                }
            }
            memStream.Position = 0;
            return memStream;
        }

        private async Task<Stream> UpdateComposerJsonAsync(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var composerJsonText = await streamReader.ReadToEndAsync();
                var jObject = JObject.Parse(composerJsonText);

                jObject["require-dev"]["friendsofphp/php-cs-fixer"] = "~2.6";

                var memStream = new MemoryStream();
                using (var streamWriter = new StreamWriter(memStream, Encoding.UTF8, 2048, true))
                {
                    await streamWriter.WriteAsync(jObject.ToString());
                }
                memStream.Position = 0;
                return memStream;
            }
        }
    }
}
