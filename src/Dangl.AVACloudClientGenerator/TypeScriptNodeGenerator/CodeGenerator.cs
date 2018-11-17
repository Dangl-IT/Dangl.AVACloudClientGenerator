using Dangl.AVACloudClientGenerator.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.TypeScriptNodeGenerator
{
    public class CodeGenerator
    {
        private readonly OptionsGenerator _optionsGenerator;
        private readonly AVACloudVersion _avaCloudVersion;
        public const string SWAGGER_GENERATOR_LANGUAGE_PARAM = "typescript-node";

        public CodeGenerator(OptionsGenerator optionsGenerator,
            AVACloudVersion avaCloudVersion)
        {
            _optionsGenerator = optionsGenerator;
            _avaCloudVersion = avaCloudVersion;
        }

        public async Task<Stream> GetGeneratedCodeZipPackageAsync(string swaggerDocumentUri)
        {
            var httpClient = new HttpClient();
            var postRequestMessage = await GetPostRequestMessageAsync(swaggerDocumentUri);
            var generatorResponse = await httpClient.SendAsync(postRequestMessage);
            var jsonResponse = await generatorResponse.Content.ReadAsStringAsync();
            var downloadLink = (string)JObject.Parse(jsonResponse)["link"];
            var generatedClientResponse = await httpClient.GetAsync(downloadLink);
            using (var generatedClientStream = await generatedClientResponse.Content.ReadAsStreamAsync())
            {
                var fileEntryModifier = new FileEntryModifier(generatedClientStream);
                return await fileEntryModifier.ReplaceDanglIdentityOAuth2Accessor();
            }
        }

        private async Task<HttpRequestMessage> GetPostRequestMessageAsync(string swaggerDocumentUri)
        {
            var typeScriptNodeClientOptions = await _optionsGenerator.GetTypescriptNodeClientGeneratorOptionsAsync(swaggerDocumentUri);
            var generatorOptions = new
            {
                swaggerUrl = swaggerDocumentUri,
                options = typeScriptNodeClientOptions
            };

            var camelCaseSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var generatorOptionsJson = JsonConvert.SerializeObject(generatorOptions, camelCaseSerializerSettings);

            var url = Constants.SWAGGER_GENERATOR_CLIENT_GEN_ENDPOINT + SWAGGER_GENERATOR_LANGUAGE_PARAM;

            return new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(generatorOptionsJson, Encoding.UTF8, "application/json")
            };
        }
    }

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
