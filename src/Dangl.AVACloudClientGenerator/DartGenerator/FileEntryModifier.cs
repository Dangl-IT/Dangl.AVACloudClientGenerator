using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.DartGenerator
{
    public class FileEntryModifier
    {
        private readonly Stream _zipArchiveStream;

        public FileEntryModifier(Stream zipArchiveStream)
        {
            _zipArchiveStream = zipArchiveStream;
        }

        public async Task<Stream> FixClientCodeAsync()
        {
            var memStream = new MemoryStream();
            await _zipArchiveStream.CopyToAsync(memStream);
            memStream.Position = 0;
            using (var archive = new ZipArchive(memStream, ZipArchiveMode.Update, true))
            {
                var projectInformationClassEntry = archive.Entries.Single(e => e.FullName.EndsWith("project_information_dto.dart"));
                using (var entryStream = projectInformationClassEntry.Open())
                {
                    using (var correctedEntryStream = await FixPriceComponentDtoJsonDeserializationAsync(entryStream))
                    {
                        projectInformationClassEntry.Delete();
                        var updatedEntry = archive.CreateEntry(projectInformationClassEntry.FullName);
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

        private async Task<Stream> FixPriceComponentDtoJsonDeserializationAsync(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var dartCode = await streamReader.ReadToEndAsync();

                // We're actually deserializing into an array, this seems to be not working properly in the generated
                // code for an array of enums
                var updatedDartCode = dartCode.Replace("priceComponentTypes: PriceComponentTypeDto.mapFromJson(json[r'priceComponentTypes']),",
                    @"priceComponentTypes: json[r'priceComponentTypes'] is Map
            ? (json[r'priceComponentTypes'] as Map).cast<String, dynamic>().map(
                (k, dynamic v) => MapEntry(k, PriceComponentTypeDto.fromJson(v) ?? PriceComponentTypeDto.unknown),
              )
            : const {},");

                if (dartCode == updatedDartCode)
                {
                      throw new InvalidOperationException("The code could not be updated");
                }

                var memStream = new MemoryStream();
                using (var streamWriter = new StreamWriter(memStream, Encoding.UTF8, 2048, true))
                {
                    await streamWriter.WriteAsync(updatedDartCode);
                }
                memStream.Position = 0;
                return memStream;
            }
        }
    }
}
