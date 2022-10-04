using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public async Task<Stream> UpdatePhpPackageAsync()
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
            _zipArchiveStream.Position = 0;
            return memStream;
        }

        private async Task<Stream> UpdateComposerJsonAsync(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var composerJsonText = await streamReader.ReadToEndAsync();
                var jObject = JObject.Parse(composerJsonText);

                jObject["require-dev"]["friendsofphp/php-cs-fixer"] = "~2.6";
                jObject["homepage"] = "https://www.dangl-it.com";
                var authors = jObject["authors"] as JArray;
                authors.Clear();
                var author = new JObject();
                author["name"] = "Dangl IT GmbH";
                author["homepage"] = "https://www.dangl-it.com";
                authors.Add(author);
                var keywords = jObject["keywords"] as JArray;
                keywords.Clear();
                keywords.Add("gaeb");
                keywords.Add("avacloud");
                keywords.Add("ava");
                keywords.Add("dangl");
                keywords.Add("bim");

                // We're going with v7 of guzzle
                jObject["require"]["guzzlehttp/guzzle"] = "^7.4.4";

                var memStream = new MemoryStream();
                using (var streamWriter = new StreamWriter(memStream, new UTF8Encoding(false), 2048, true))
                {
                    var jsonString = jObject.ToString();
                    await streamWriter.WriteAsync(jsonString);
                }
                memStream.Position = 0;
                return memStream;
            }
        }

        public async Task<Stream> UpdateReadmeAsync()
        {
            var memStream = new MemoryStream();
            await _zipArchiveStream.CopyToAsync(memStream);
            memStream.Position = 0;
            using (var archive = new ZipArchive(memStream, ZipArchiveMode.Update, true))
            {
                var readmeEntry = archive.Entries.Single(e => e.FullName.EndsWith("AVACloud/README.md"));
                using (var entryStream = readmeEntry.Open())
                {
                    using (var correctedStream = await UpdateReadmeAsync(entryStream))
                    {
                        readmeEntry.Delete();
                        var updateEntry = archive.CreateEntry(readmeEntry.FullName);
                        using (var updatedEntryStream = updateEntry.Open())
                        {
                            await correctedStream.CopyToAsync(updatedEntryStream);
                        }
                    }
                }
            }
            memStream.Position = 0;
            _zipArchiveStream.Position = 0;
            return memStream;
        }

        private async Task<Stream> UpdateReadmeAsync(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var readmeText = await streamReader.ReadToEndAsync();
                readmeText = Regex.Replace(readmeText, "\r\n?|\n", "\r\n");
                if (!readmeText.Contains("https://github.com/dangl/avacloud.git"))
                {
                    throw new Exception("Did not find the expected string to be replaced in the README.md: https://github.com/dangl/avacloud.git");
                }
                readmeText = readmeText.Replace("https://github.com/dangl/avacloud.git", "https://github.com/Dangl-IT/avacloud-client-php.git");

                if (!readmeText.Contains("# Dangl\\AVACloud\r\n"))
                {
                    throw new Exception("Did not find the expected string to be replaced in the README.md: # Dangl\\AVACloud (with a CRLF line ending)");
                }
                readmeText = readmeText.Replace("# Dangl\\AVACloud\r\n", "# Dangl\\AVACloud\r\n" +
                    "Please see the offical site for more information and further documentation: [https://www.dangl-it.com/products/avacloud-gaeb-saas/](https://www.dangl-it.com/products/avacloud-gaeb-saas/)  \r\n" +
                    "To get started, you can use the PHP demo application: [https://github.com/Dangl-IT/avacloud-demo-php](https://github.com/Dangl-IT/avacloud-demo-php)\r\n\r\n");

                var memStream = new MemoryStream();
                using (var streamWriter = new StreamWriter(memStream, new UTF8Encoding(false), 2048, true))
                {
                    await streamWriter.WriteAsync(readmeText);
                }
                memStream.Position = 0;
                return memStream;
            }
        }
    }
}
