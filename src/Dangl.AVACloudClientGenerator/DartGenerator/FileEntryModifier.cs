using System;
using System.Collections.Generic;
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

                var apiErrorClassEntry = archive.Entries.Single(e => e.FullName.EndsWith("api_error.dart"));
                using (var entryStream = apiErrorClassEntry.Open())
                {
                    using (var correctedEntryStream = await FixApiErrorDeserializationAsync(entryStream))
                    {
                        apiErrorClassEntry.Delete();
                        var updatedEntry = archive.CreateEntry(apiErrorClassEntry.FullName);
                        using (var updatedEntrystream = updatedEntry.Open())
                        {
                            await correctedEntryStream.CopyToAsync(updatedEntrystream);
                        }
                    }
                }

                var iElementEntries = new Dictionary<string, string>
                {
                    {"PositionDto", "position_dto.dart" },
                    {"NoteTextDto", "note_text_dto.dart" },
                    {"ServiceSpecificationGroupDto", "service_specification_group_dto.dart" },
                    {"ExecutionDescriptionDto", "execution_description_dto.dart" }
                };
                foreach (var iElementEntry in iElementEntries)
                {
                    var zipEntry = archive.Entries.Single(e => e.FullName.EndsWith(iElementEntry.Value));
                    using (var entryStream = zipEntry.Open())
                    {
                        using (var correctedEntryStream = await AddIElementDtoInheritanceAsync(entryStream, iElementEntry.Key))
                        {
                            zipEntry.Delete();
                            var updatedEntry = archive.CreateEntry(zipEntry.FullName);
                            using (var updatedEntrystream = updatedEntry.Open())
                            {
                                await correctedEntryStream.CopyToAsync(updatedEntrystream);
                            }
                        }
                    }
                }

                var iElementDtoEntry = archive.Entries.Single(e => e.FullName.EndsWith("i_element_dto.dart"));
                using (var entryStream = iElementDtoEntry.Open())
                {
                    using (var correctedEntryStream = await FixIElementDtoEntryAsync(entryStream))
                    {
                        iElementDtoEntry.Delete();
                        var updatedEntry = archive.CreateEntry(iElementDtoEntry.FullName);
                        using (var updatedEntrystream = updatedEntry.Open())
                        {
                            await correctedEntryStream.CopyToAsync(updatedEntrystream);
                        }
                    }
                }

                var allEntries = archive.Entries.Where(e => e.FullName.EndsWith(".dart")).ToList();
                foreach (var entry in allEntries)
                {
                    using var entryStream = entry.Open();
                    using var correctedEntryStream = await FixParsingOfNullableNumerics(entryStream);
                    entry.Delete();
                    var updatedEntry = archive.CreateEntry(entry.FullName);
                    using var updatedEntrystream = updatedEntry.Open();
                    await correctedEntryStream.CopyToAsync(updatedEntrystream);
                }
            }
            memStream.Position = 0;
            return memStream;
        }

        private async Task<Stream> FixIElementDtoEntryAsync(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var dartCode = await streamReader.ReadToEndAsync();

                // Use parameterless constructor We never instantiate the interface IElementDto, so
                // we just generate it from the sub classes
                var dartLines = Regex.Split(dartCode, @"\r\n?|\n");
                var updatedDartCode = string.Empty;
                var isInConstructor = false;
                foreach (var line in dartLines)
                {
                    if (isInConstructor)
                    {
                        if (line.Trim().StartsWith("});"))
                        {
                            isInConstructor = false;
                        }
                    }
                    else
                    {
                        if (line.Trim().StartsWith("IElementDto({"))
                        {
                            isInConstructor = true;
                            updatedDartCode += "IElementDto()" + Environment.NewLine;
                            updatedDartCode += ": id = ''," + Environment.NewLine;
                            updatedDartCode += "elementTypeDiscriminator = ''," + Environment.NewLine;
                            updatedDartCode += "projectCatalogues = const []," + Environment.NewLine;
                            updatedDartCode += "catalogueReferences = const[];" + Environment.NewLine;
                        }
                        else
                        {
                            updatedDartCode += line + Environment.NewLine;
                        }
                    }
                }

                if (dartCode == updatedDartCode)
                {
                    throw new InvalidOperationException("The code could not be updated");
                }

                dartCode = updatedDartCode;

                // Change deserialization method
                dartLines = Regex.Split(dartCode, @"\r\n?|\n");
                updatedDartCode = string.Empty;
                var isInDeserialization = false;
                foreach (var line in dartLines)
                {
                    if (isInDeserialization)
                    {
                        if (line.Trim() == "}")
                        {
                            isInDeserialization = false;
                            updatedDartCode += line + Environment.NewLine;
                        }
                    }
                    else
                    {
                        updatedDartCode += line + Environment.NewLine;
                        if (line.Trim().StartsWith("final json = value.cast<String, dynamic>();"))
                        {
                            isInDeserialization = true;
                            updatedDartCode += @"      var elementType = mapValueOfType<String>(json, r'elementTypeDiscriminator')!;
      switch (elementType) {
        case 'Position':
        case 'PositionDto':
          return PositionDto.fromJson(json);

        case 'ServiceSpecificationGroup':
        case 'ServiceSpecificationGroupDto':
          return ServiceSpecificationGroupDto.fromJson(json);

        case 'NoteText':
        case 'NoteTextDto':
          return NoteTextDto.fromJson(json);

        case 'ExecutionDescription':
        case 'ExecutionDescriptionDto':
          return ExecutionDescriptionDto.fromJson(json);
      }" + Environment.NewLine;
                        }
                    }
                }

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

        private async Task<Stream> FixPriceComponentDtoJsonDeserializationAsync(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var dartCode = await streamReader.ReadToEndAsync();

                // We're actually deserializing into an array, this seems to be not working properly
                // in the generated code for an array of enums
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

        private async Task<Stream> FixApiErrorDeserializationAsync(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var dartCode = await streamReader.ReadToEndAsync();

                // We're actually deserializing into an array, this seems to be not working properly
                // in the generated code for an array of enums
                var updatedDartCode = dartCode.Replace(": mapCastOfType<String, List>(json, r'errors'),",
                    @": (json[r'errors'] as Map<String, dynamic>).map(
              (key, value) => MapEntry(key, List<String>.from(value as List))),");

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

        private async Task<Stream> FixParsingOfNullableNumerics(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var dartCode = await streamReader.ReadToEndAsync();

                var nullableNumericTypes = Regex.Matches(dartCode, @"\s+num\? ([a-z][a-zA-Z0-9]+);");
                if (nullableNumericTypes.Any())
                {
                    foreach (var nullableNumeric in nullableNumericTypes.OfType<System.Text.RegularExpressions.Match>())
                    {
                        dartCode = dartCode.Replace($"{nullableNumeric.Groups[1].Value}: num.parse(", $"{nullableNumeric.Groups[1].Value}: num.tryParse(");
                    }
                }

                var memStream = new MemoryStream();
                using (var streamWriter = new StreamWriter(memStream, Encoding.UTF8, 2048, true))
                {
                    await streamWriter.WriteAsync(dartCode);
                }
                memStream.Position = 0;
                return memStream;
            }
        }

        private async Task<Stream> AddIElementDtoInheritanceAsync(Stream fileStream, string className)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var dartCode = await streamReader.ReadToEndAsync();

                // We're actually deserializing into an array, this seems to be not working properly
                // in the generated code for an array of enums
                var updatedDartCode = dartCode.Replace($"class {className} {{",
                    $"class {className} extends IElementDto {{");

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
