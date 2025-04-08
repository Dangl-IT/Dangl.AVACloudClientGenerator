using Dangl.AVACloudClientGenerator.Shared;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator
{
    public sealed class ClientGenerator : IDisposable
    {
        private readonly ClientGeneratorOptions _clientGeneratorOptions;
        private readonly AVACloudVersion _avaCloudVersion = new AVACloudVersion();
        private Stream _zippedClientCodeStream;

        public ClientGenerator(ClientGeneratorOptions clientGeneratorOptions)
        {
            _clientGeneratorOptions = clientGeneratorOptions;
        }

        public void Dispose()
        {
            _zippedClientCodeStream?.Dispose();
        }

        public async Task GenerateClientCodeAsync()
        {
            if (!string.IsNullOrWhiteSpace(_clientGeneratorOptions.SwaggerGeneratorClientGenEndpoint))
            {
                await EnsureDockerContainerIsAlreadyAvailableAsync();
            }

            var swaggerDocumentUri = string.IsNullOrWhiteSpace(_clientGeneratorOptions.SwaggerDocUri)
                ? Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT
                : _clientGeneratorOptions.SwaggerDocUri;
            var shouldAddReadme = true;
            switch (_clientGeneratorOptions.ClientLanguage)
            {
                case ClientLanguage.Java:
                    await GenerateJavaClient(swaggerDocumentUri);
                    break;

                case ClientLanguage.TypeScriptNode:
                    await GenerateTypeScriptNodeClient(swaggerDocumentUri);
                    shouldAddReadme = false;
                    break;

                case ClientLanguage.JavaScript:
                    await GenerateJavaScriptClient(swaggerDocumentUri);
                    break;

                case ClientLanguage.Php:
                    await GeneratePhpClient(swaggerDocumentUri);
                    break;

                case ClientLanguage.Python:
                    await GeneratePythonClient(swaggerDocumentUri);
                    break;

                case ClientLanguage.Dart:
                    await GenerateDartClient(swaggerDocumentUri);
                    break;

                default:
                    throw new NotImplementedException("The specified language is not supported");
            }

            await WriteClientCodeAsync(shouldAddReadme);
        }

        private async Task GenerateJavaClient(string swaggerDocumentUri)
        {
            var javaOptionsGenerator = new JavaGenerator.OptionsGenerator(_avaCloudVersion);
            var javaGenerator = new JavaGenerator.CodeGenerator(javaOptionsGenerator, _avaCloudVersion);
            _zippedClientCodeStream = await javaGenerator.GetGeneratedCodeZipPackageAsync(swaggerDocumentUri, _clientGeneratorOptions.SwaggerGeneratorClientGenEndpoint);
        }

        private async Task GenerateTypeScriptNodeClient(string swaggerDocumentUri)
        {
            var typeScriptNodeGenerator = new TypeScriptNodeGenerator.CodeGenerator(_avaCloudVersion);
            _zippedClientCodeStream = await typeScriptNodeGenerator.GetGeneratedCodeZipPackageAsync(swaggerDocumentUri);
        }

        private async Task GenerateJavaScriptClient(string swaggerDocumentUri)
        {
            var javaScriptOptionsGenerator = new JavaScriptGenerator.OptionsGenerator(_avaCloudVersion);
            var javaScriptGenerator = new JavaScriptGenerator.CodeGenerator(javaScriptOptionsGenerator, _avaCloudVersion);
            _zippedClientCodeStream = await javaScriptGenerator.GetGeneratedCodeZipPackageAsync(swaggerDocumentUri, _clientGeneratorOptions.SwaggerGeneratorClientGenEndpoint);
        }

        private async Task GeneratePhpClient(string swaggerDocumentUri)
        {
            var phpOptionsGenerator = new PhpGenerator.OptionsGenerator(_avaCloudVersion);
            var phpGenerator = new PhpGenerator.CodeGenerator(phpOptionsGenerator, _avaCloudVersion);
            _zippedClientCodeStream = await phpGenerator.GetGeneratedCodeZipPackageAsync(swaggerDocumentUri, _clientGeneratorOptions.SwaggerGeneratorClientGenEndpoint);
        }

        private async Task GeneratePythonClient(string swaggerDocumentUri)
        {
            var pythonOptionsGenerator = new PythonGenerator.OptionsGenerator(_avaCloudVersion);
            var pythonGenerator = new PythonGenerator.CodeGenerator(pythonOptionsGenerator, _avaCloudVersion);
            _zippedClientCodeStream = await pythonGenerator.GetGeneratedCodeZipPackageAsync(swaggerDocumentUri, _clientGeneratorOptions.SwaggerGeneratorClientGenEndpoint);
        }

        private async Task GenerateDartClient(string swaggerDocumentUri)
        {
            var dartOptionsGenerator = new DartGenerator.OptionsGenerator(_avaCloudVersion);
            var dartGenerator = new DartGenerator.CodeGenerator(dartOptionsGenerator, _avaCloudVersion);
            _zippedClientCodeStream = await dartGenerator.GetGeneratedCodeZipPackageAsync(swaggerDocumentUri);
        }

        private async Task WriteClientCodeAsync(bool shouldAddReadme)
        {
            await new OutputWriter(_zippedClientCodeStream, _clientGeneratorOptions.OutputPathFolder)
                .WriteCodeToDirectoryAndAddReadmeAndLicense(shouldAddReadme);
        }

        private async Task EnsureDockerContainerIsAlreadyAvailableAsync()
        {
            var maxRetries = 3;
            HttpResponseMessage generatorResponse = null;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var httpClient = new HttpClient
                    {
                        Timeout = TimeSpan.FromMinutes(5)
                    };
                    generatorResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, _clientGeneratorOptions.SwaggerGeneratorClientGenEndpoint));
                    generatorResponse.EnsureSuccessStatusCode();
                    break;
                }
                catch (HttpRequestException) when (i < maxRetries - 1)
                {
                    await Task.Delay(2000);
                }
            }

            if (generatorResponse == null || !generatorResponse.IsSuccessStatusCode)
            {
                throw new Exception("Failed to get a successful response from the generator.");
            }
        }
    }
}
