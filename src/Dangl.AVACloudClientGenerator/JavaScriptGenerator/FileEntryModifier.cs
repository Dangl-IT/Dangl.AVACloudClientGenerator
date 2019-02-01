using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.JavaScriptGenerator
{
    public class FileEntryModifier
    {
        private readonly Stream _zipArchiveStream;

        public FileEntryModifier(Stream zipArchiveStream)
        {
            _zipArchiveStream = zipArchiveStream;
        }

        public async Task<Stream> UpdatePackageJsonAndFixInheritanceAsync()
        {
            var memStream = new MemoryStream();
            await _zipArchiveStream.CopyToAsync(memStream);
            memStream.Position = 0;
            using (var archive = new ZipArchive(memStream, ZipArchiveMode.Update, true))
            {
                var packageJsonEntry = archive.Entries.Single(e => e.FullName.EndsWith("package.json"));
                using (var entryStream = packageJsonEntry.Open())
                {
                    using (var correctedStream = await UpdatePackageJsonAsync(entryStream))
                    {
                        packageJsonEntry.Delete();
                        var updateEntry = archive.CreateEntry(packageJsonEntry.FullName);
                        using (var updatedEntryStream = updateEntry.Open())
                        {
                            await correctedStream.CopyToAsync(updatedEntryStream);
                        }
                    }
                }

                var dtoEntries = archive.Entries.Where(e => e.FullName.EndsWith("Dto.js")).ToList();
                foreach (var dtoEntry in dtoEntries)
                {
                    using (var entryStream = dtoEntry.Open())
                    {
                        using (var correctedStream = await FixInheritanceAsync(entryStream))
                        {
                            dtoEntry.Delete();
                            var updateEntry = archive.CreateEntry(dtoEntry.FullName);
                            using (var updatedEntryStream = updateEntry.Open())
                            {
                                await correctedStream.CopyToAsync(updatedEntryStream);
                            }
                        }
                    }
                }

                var apiClientEntry = archive.Entries.Single(e => e.FullName.EndsWith("ApiClient.js"));
                using (var entryStream = apiClientEntry.Open())
                {
                    using (var correctedStream = await FixFileResponseConversionAsync(entryStream))
                    {
                        apiClientEntry.Delete();
                        var updatedEntry = archive.CreateEntry(apiClientEntry.FullName);
                        using (var updatedEntryStream = updatedEntry.Open())
                        {
                            await correctedStream.CopyToAsync(updatedEntryStream);
                        }
                    }
                }

            }
            memStream.Position = 0;
            return memStream;
        }

        private async Task<Stream> UpdatePackageJsonAsync(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var packageJsonText = await streamReader.ReadToEndAsync();
                var jObject = JObject.Parse(packageJsonText);

                jObject["scripts"]["build"] = "browserify ./src/index.js --standalone DanglAVACloudClient > bundle.js&&uglifyjs bundle.js > bundle.min.js";
                jObject["license"] = "LICENSE.md";
                jObject["devDependencies"]["browserify"] = "16.2.3";
                jObject["devDependencies"]["uglify-js"] = "3.4.9";

                var memStream = new MemoryStream();
                using (var streamWriter = new StreamWriter(memStream, Encoding.UTF8, 2048, true))
                {
                    await streamWriter.WriteAsync(jObject.ToString());
                }
                memStream.Position = 0;
                return memStream;
            }
        }

        private async Task<Stream> FixInheritanceAsync(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var fileContent = await streamReader.ReadToEndAsync();
                var invalidObjectAssignmentMethodStart = "exports.constructFromObject = function(data, obj) {";
                var startIndex = fileContent.IndexOf(invalidObjectAssignmentMethodStart);
                if (startIndex == -1)
                {
                    var originalStream = new MemoryStream();
                    using (var streamWriter = new StreamWriter(originalStream, Encoding.UTF8, 2048, true))
                    {
                        await streamWriter.WriteAsync(fileContent);
                    }
                    originalStream.Position = 0;
                    return originalStream;
                }
                startIndex += invalidObjectAssignmentMethodStart.Length;
                int parentheses = 1;
                int endIndex = startIndex;
                while (parentheses > 0)
                {
                    if (fileContent[endIndex] == '{')
                    {
                        parentheses++;
                    }
                    else if (fileContent[endIndex] == '}')
                    {
                        parentheses--;
                    }
                    endIndex++;
                }

                var modifiedContent = fileContent.Substring(0, startIndex)
                    + Environment.NewLine
                    + "    // This function was modified by the Dangl.AVACloudClientGenerator to return the plain object"
                    + Environment.NewLine
                    + "    // Otherwise, the generated Swagger client for JavaScript would only construct the abstract base type IElementDto"
                    + Environment.NewLine
                    + "    // and ignore polymorphism, meaning it would just not assign the child properties"
                    + Environment.NewLine
                    + "    return data;"
                    + Environment.NewLine
                    + "  }"
                    + fileContent.Substring(endIndex);

                var memStream = new MemoryStream();
                using (var streamWriter = new StreamWriter(memStream, Encoding.UTF8, 2048, true))
                {
                    await streamWriter.WriteAsync(modifiedContent);
                }
                memStream.Position = 0;
                return memStream;
            }
        }

        private async Task<Stream> FixFileResponseConversionAsync(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var apiClientText = await streamReader.ReadToEndAsync();

                apiClientText = apiClientText.Replace("(type === Object)", "(type === Object || type === File)");

                var memStream = new MemoryStream();
                using (var streamWriter = new StreamWriter(memStream, Encoding.UTF8, 2048, true))
                {
                    await streamWriter.WriteAsync(apiClientText);
                }
                memStream.Position = 0;
                return memStream;
            }
        }
    }
}
