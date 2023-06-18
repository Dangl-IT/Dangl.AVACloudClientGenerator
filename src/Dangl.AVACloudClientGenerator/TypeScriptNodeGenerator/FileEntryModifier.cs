using Dangl.AVACloudClientGenerator.Utilities;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
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

        public async Task<Stream> GenerateOverloadsWithOptionsObjectAsync(Stream fileStream)
        {
            var memStream = new MemoryStream();
            await fileStream.CopyToAsync(memStream);
            memStream.Position = 0;
            using (var archive = new ZipArchive(memStream, ZipArchiveMode.Update, true))
            {
                var apiDefinitionEntry = archive.Entries.Single(e => e.FullName.EndsWith("api.ts"));
                using var correctedEntryStream = new MemoryStream();
                using (var entryStream = apiDefinitionEntry.Open())
                {
                    string updatedCode;
                    using (var streamReader = new StreamReader(entryStream))
                    {
                        var originalCode = await streamReader.ReadToEndAsync();
                        updatedCode = originalCode.GenerateMethodOverloadsWithOptionsObject();
                    }

                    using (var streamWriter = new StreamWriter(correctedEntryStream, Encoding.UTF8, 2048, true))
                    {
                        await streamWriter.WriteAsync(updatedCode);
                    }
                    correctedEntryStream.Position = 0;
                }
                apiDefinitionEntry.Delete();
                var updatedEntry = archive.CreateEntry(apiDefinitionEntry.FullName);

                using (var updatedEntrystream = updatedEntry.Open())
                {
                    await correctedEntryStream.CopyToAsync(updatedEntrystream);
                }
            }
            
            memStream.Position = 0;
            return memStream;
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
                    using (var correctedEntryStream = await ReplaceDanglIdentityAccessorInFileAndFixDuplicatedJsonValueInRequest(entryStream))
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

        private async Task<Stream> ReplaceDanglIdentityAccessorInFileAndFixDuplicatedJsonValueInRequest(Stream fileStream)
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
                    .Replace("oenormFile?: Buffer", "oenormFile: FileParameter") // Buffer doesnt include filename
                    .Replace("rebFile?: Buffer", "rebFile: FileParameter") // Buffer doesnt include filename
                    .Replace("siaFile?: Buffer", "siaFile: FileParameter") // Buffer doesnt include filename
                    .Replace("aslvFile?: Buffer", "aslvFile: FileParameter") // Buffer doesnt include filename
                    .Replace("avaFile?: Buffer", "avaFile: FileParameter") // Buffer doesnt include filename
                    .Replace("datanormFile?: Buffer", "datanormFile: FileParameter") // Buffer doesnt include filename
                    .Replace("uglFile?: Buffer", "uglFile: FileParameter") // Buffer doesnt include filename
                    .Replace("datanormFile: Buffer", "datanormFile: FileParameter") // Buffer doesnt include filename
                    .Replace("uglFile: Buffer", "uglFile: FileParameter") // Buffer doesnt include filename
                    .Replace("idsConnectFile?: Buffer", "idsConnectFile: FileParameter") // Buffer doesnt include filename
                    .Replace("idsConnectFile: Buffer", "idsConnectFile: FileParameter") // Buffer doesnt include filename
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

                // The following fixes the deserialization of the returned data if it's a ProjectDto
                // The generated Swagger code does not produce correct results for Enumerations, instead
                // they're mapped to an object, thus the serialization produces a format that is rejected
                // by the AVACloud API endpoint
                typeScriptCode = typeScriptCode.Replace("public static deserialize(data: any, type: string) {",
                    "public static deserialize(data: any, type: string) {"
                    + "\r\n        if (type === 'ProjectDto') {"
                    + "\r\n            return data;"
                    + "\r\n        }");

                // There's been a bug in the generated code where the localVarRequestOptions object
                // had the attribute for Json set twice
                typeScriptCode = Regex.Replace(typeScriptCode, "json: true,(\r\n?|\n)\\s+body: avaProject,(\r\n?|\n)\\s+json: true", "json: true,\r\n            body: avaProject");

                var memStream = new MemoryStream();
                using (var streamWriter = new StreamWriter(memStream, Encoding.UTF8, 2048, true))
                {
                    await streamWriter.WriteAsync(typeScriptCode);
                }
                memStream.Position = 0;
                return memStream;
            }
        }

        public async Task<Stream> EnableCommentsInTsconfig(Stream fileStream)
        {
            var memStream = new MemoryStream();
            await fileStream.CopyToAsync(memStream);
            memStream.Position = 0;
            using (var archive = new ZipArchive(memStream, ZipArchiveMode.Update, true))
            {
                var tsconfigEntry = archive.Entries.Single(e => e.FullName.EndsWith("tsconfig.json"));
                using (var entryStream = tsconfigEntry.Open())
                {
                    using (var correctedEntryStream = await UpdateTsConfigAndEnableComments(entryStream))
                    {
                        tsconfigEntry.Delete();
                        var updatedEntry = archive.CreateEntry(tsconfigEntry.FullName);

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

        private async Task<Stream> UpdateTsConfigAndEnableComments(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var fileContent = await streamReader.ReadToEndAsync();
                fileContent = fileContent.Replace("\"removeComments\": true", "\"removeComments\": false");
                var memStream = new MemoryStream();
                using (var streamWriter = new StreamWriter(memStream, new UTF8Encoding(false), 2048, true))
                {
                    await streamWriter.WriteAsync(fileContent);
                }
                memStream.Position = 0;
                return memStream;
            }
        }

        public async Task<Stream> UpdateTypeScriptVersion(Stream fileStream)
        {
            var memStream = new MemoryStream();
            await fileStream.CopyToAsync(memStream);
            memStream.Position = 0;
            using (var archive = new ZipArchive(memStream, ZipArchiveMode.Update, true))
            {
                var apiDefinitionEntry = archive.Entries.Single(e => e.FullName.EndsWith("package.json"));
                using (var entryStream = apiDefinitionEntry.Open())
                {
                    using (var correctedEntryStream = await UpdateTypeScriptVersionInPackageJson(entryStream))
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

        private async Task<Stream> UpdateTypeScriptVersionInPackageJson(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var fileContent = await streamReader.ReadToEndAsync();
                var originalFileContent = fileContent;

                fileContent = fileContent
                    .Replace("\"typescript\": \"^2.4.2\"", "\"typescript\": \"^4.9.4\"");

                if (fileContent == originalFileContent)
                {
                    throw new Exception("Failed to fix the package.json, while updating the TypeScript version.");
                }

                var memStream = new MemoryStream();
                using (var streamWriter = new StreamWriter(memStream, new UTF8Encoding(false), 2048, true))
                {
                    await streamWriter.WriteAsync(fileContent);
                }
                memStream.Position = 0;
                return memStream;
            }
        }

        public async Task<Stream> EnsureIElementDtoIsDeclaredBeforeUsed(Stream fileStream)
        {
            var memStream = new MemoryStream();
            await fileStream.CopyToAsync(memStream);
            memStream.Position = 0;
            using (var archive = new ZipArchive(memStream, ZipArchiveMode.Update, true))
            {
                var apiDefinitionEntry = archive.Entries.Single(e => e.FullName.EndsWith("api.ts"));
                using (var entryStream = apiDefinitionEntry.Open())
                {
                    using (var correctedEntryStream = await UpdateApiToMoveIElementDeclarationBeforeUsage(entryStream))
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

        public static async Task<Stream> UpdateApiToMoveIElementDeclarationBeforeUsage(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream))
            {
                var fileContent = await streamReader.ReadToEndAsync();
                var originalFileContent = fileContent;


                var lines = Regex.Split(fileContent, "\r\n?|\n")
                    .Select((line, index) => new { line, index })
                    .ToList();

                var firstLineIndexThatMentionsIElement = lines
                    .FirstOrDefault(l => l.line.Contains("extends IElementDto"))
                    .index;

                var iElementDtoDeclarationLineIndex = lines
                    .FirstOrDefault(l => l.line.Contains("export class IElementDto"))
                    .index;

                var hasFoundEmptyLine = false;
                while (!hasFoundEmptyLine)
                {
                    var line = lines[iElementDtoDeclarationLineIndex];
                    if (string.IsNullOrWhiteSpace(line.line))
                    {
                        hasFoundEmptyLine = true;
                    }
                    else
                    {
                        iElementDtoDeclarationLineIndex--;
                    }
                }

                var endIndexIElementDtoDefinition = iElementDtoDeclarationLineIndex;
                var hasFoundClosingBracket = false;
                while (!hasFoundClosingBracket)
                {
                    var line = lines[endIndexIElementDtoDefinition];
                    if (line.line == "}")
                    {
                        hasFoundClosingBracket = true;
                    }
                    endIndexIElementDtoDefinition++;
                }

                if (iElementDtoDeclarationLineIndex < firstLineIndexThatMentionsIElement)
                {
                    // If this is entered, the file is already correctly structured
                    var originalMemStream = new MemoryStream();
                    using (var streamWriter = new StreamWriter(originalMemStream, new UTF8Encoding(false), 2048, true))
                    {
                        await streamWriter.WriteAsync(fileContent);
                    }
                    originalMemStream.Position = 0;
                    return originalMemStream;
                }

                var iElementDtoDeclarationLines = lines
                    .SkipWhile(l => l.index <= iElementDtoDeclarationLineIndex)
                    .TakeWhile(l => l.index <= endIndexIElementDtoDefinition)
                    .Select(l => l.line);

                var linesBefore = lines
                    .TakeWhile(l => l.index < iElementDtoDeclarationLineIndex
                    && l.index < firstLineIndexThatMentionsIElement)
                    .Select(l => l.line);

                var linesAfter = lines
                    .SkipWhile(l => l.index < firstLineIndexThatMentionsIElement)
                    .Where(l => l.index <= iElementDtoDeclarationLineIndex
                        || l.index > endIndexIElementDtoDefinition)
                    .Select(l => l.line);

                var stringWriter = new StringWriter();

                var newOrderedLines = linesBefore
                    .Concat(iElementDtoDeclarationLines)
                    .Concat(linesAfter);
                foreach (var line in newOrderedLines)
                {
                    stringWriter.WriteLine(line);
                }

                var memStream = new MemoryStream();
                using (var streamWriter = new StreamWriter(memStream, new UTF8Encoding(false), 2048, true))
                {
                    await streamWriter.WriteAsync(stringWriter.ToString());
                }
                memStream.Position = 0;
                return memStream;
            }
        }
    }
}
