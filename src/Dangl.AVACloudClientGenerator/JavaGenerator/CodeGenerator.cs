using Dangl.AVACloudClientGenerator.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.JavaGenerator
{
    public class CodeGenerator
    {
        private readonly OptionsGenerator _optionsGenerator;
        private readonly AVACloudVersion _avaCloudVersion;
        public const string SWAGGER_GENERATOR_LANGUAGE_PARAM = "java";

        public CodeGenerator(OptionsGenerator optionsGenerator,
            AVACloudVersion avaCloudVersion)
        {
            _optionsGenerator = optionsGenerator;
            _avaCloudVersion = avaCloudVersion;
        }

        public async Task<Stream> GetGeneratedCodeZipPackageAsync(string swaggerDocumentUri, string swaggerGeneratorClientGenEndpoint)
        {
            if (string.IsNullOrWhiteSpace(swaggerGeneratorClientGenEndpoint))
            {
                throw new Exception($"The {SWAGGER_GENERATOR_LANGUAGE_PARAM} client generator requires a Swagger generator client gen endpoint.");
            }

            var httpClient = new HttpClient();
            var postRequestMessage = await GetPostRequestMessageAsync(swaggerDocumentUri, swaggerGeneratorClientGenEndpoint);
            var generatorResponse = await httpClient.SendAsync(postRequestMessage);
            var jsonResponse = await generatorResponse.Content.ReadAsStringAsync();
            var uri = new Uri(swaggerGeneratorClientGenEndpoint);
            var downloadLink = ((string)JObject.Parse(jsonResponse)["link"]).Replace("https://generator.swaggerhub.com/api/swagger.json", $"{uri.Scheme}://{uri.Host}:{uri.Port}");
            var generatedClientResponse = await httpClient.GetAsync(downloadLink);
            if (!generatedClientResponse.IsSuccessStatusCode)
            {
                var errorMessage = await generatedClientResponse.Content.ReadAsStringAsync();
                throw new Exception("Error during download of generated client from Swagger, status code: " + generatedClientResponse.StatusCode + Environment.NewLine + errorMessage);
            }
            var generatedClientStream = await generatedClientResponse.Content.ReadAsStreamAsync();
            return generatedClientStream;
        }

        private async Task<HttpRequestMessage> GetPostRequestMessageAsync(string swaggerDocumentUri, string swaggerGeneratorClientGenEndpoint)
        {
            var javaClientOptions = await _optionsGenerator.GetJavaClientGeneratorOptionsAsync(swaggerDocumentUri);
            var generatorOptions = new
            {
                swaggerUrl = swaggerDocumentUri,
                options = javaClientOptions
            };

            var camelCaseSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var generatorOptionsJson = JsonConvert.SerializeObject(generatorOptions, camelCaseSerializerSettings);

            var url = swaggerGeneratorClientGenEndpoint + SWAGGER_GENERATOR_LANGUAGE_PARAM;

            return new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(generatorOptionsJson, Encoding.UTF8, "application/json")
            };
        }
    }
}
