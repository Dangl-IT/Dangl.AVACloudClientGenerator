using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.TypeScriptNodeGenerator
{
    public class FileEntryModifier
    {
        private readonly Stream _zipArchiveStream;

        public FileEntryModifier(Stream zipArchiveStream)
        {
            _zipArchiveStream = zipArchiveStream;
        }

        public async Task<Stream> ReplaceDanglIdentityOAuth2Accessor()
        {
            var memStream = new MemoryStream();
            await _zipArchiveStream.CopyToAsync(memStream);
            memStream.Position = 0;
            using (var archive = new ZipArchive(memStream, ZipArchiveMode.Update, true))
            {
                var apiDefinitionEntry = archive.Entries.Single(e => e.FullName.EndsWith("api.ts"));
                using (var entryStream = apiDefinitionEntry.Open())
                {
                    using (var correctedEntryStream = await ReplaceDanglIdentityAccessorInFile(entryStream))
                    {
                        apiDefinitionEntry.Delete();
                        var updatedEntry = archive.CreateEntry(apiDefinitionEntry.FullName);

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

        private async Task<Stream> ReplaceDanglIdentityAccessorInFile(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var typeScriptCode = await streamReader.ReadToEndAsync();
                typeScriptCode = typeScriptCode
                    .Replace("this.authentications.Dangl.Identity", "this.authentications['Dangl.Identity']") // Fix OAuth2 scheme name
                    .Replace("static discriminator = elementTypeDiscriminator;", "static discriminator = 'elementTypeDiscriminator';") // Disriminator fix
                    .Replace("static discriminator = undefined;", "static discriminator = '';") // Discriminator fix
                    .Replace("public username: string;", "public username: string | undefined;") // Uninitialized variable
                    .Replace("public password: string;", "public password: string | undefined;") // Uninitialized variable
                    .Replace("public apiKey: string;", "public apiKey: string | undefined;") // Uninitialized variable
                    .Replace("public accessToken: string;", "public accessToken: string | undefined;") // Uninitialized variable
                    .Replace("excelFile: Buffer", "excelFile: FileParameter") // Buffer doesnt include filename
                    .Replace("gaebFile: Buffer", "gaebFile: FileParameter") // Buffer doesnt include filename
                    .Replace("excelFile?: Buffer", "excelFile: FileParameter") // Buffer doesnt include filename
                    .Replace("gaebFile?: Buffer", "gaebFile: FileParameter") // Buffer doesnt include filename
                    + Environment.NewLine // The FileParameter should be used instead of a raw Buffer, otherwise
                                          // no filename is included in the request and AVACloud rejects the request with a 400 error
                    + @"
export interface FileParameter {
  value: Buffer,
  options: {
    filename: string,
    contentType: string
  }
}" + Environment.NewLine;

                // The following fixes two things:
                // 1. The 'ObjectSerializer.serialize() method does not correctly work with the abstract IElementDto. It only serializes the shared
                // properties, thus missing all properties not present on the shared, abstract base class
                // 2. Additionally, the generated code was missing the option 'json: true' when sending requests, so the serializer 
                // form the 'requests' package threw an exception
                typeScriptCode = Regex.Replace(typeScriptCode, "([ ]+)body: ObjectSerializer\\.serialize\\(([a-zA-Z]+), \"ProjectDto\"\\)", "$1body: $2,\r\n$1json: true");

                var memStream = new MemoryStream();
                using (var streamWriter = new StreamWriter(memStream, Encoding.UTF8, 2048, true))
                {
                    await streamWriter.WriteAsync(typeScriptCode);
                }
                memStream.Position = 0;
                return memStream;
            }
        }
    }
}
