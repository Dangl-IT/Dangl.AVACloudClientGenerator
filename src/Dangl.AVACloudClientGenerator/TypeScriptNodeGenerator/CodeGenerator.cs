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
                var stream = await fileEntryModifier.ReplaceDanglIdentityOAuth2Accessor();
                stream = await fileEntryModifier.EnableCommentsInTsConfig(stream);
                stream = await fileEntryModifier.UpdateTypeScriptVersion(stream);
                stream = await fileEntryModifier.EnsureIElementDtoIsDeclaredBeforeUsed(stream);
                stream = await fileEntryModifier.GenerateOverloadsWithOptionsObjectAsync(stream);
                return stream;
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
}
