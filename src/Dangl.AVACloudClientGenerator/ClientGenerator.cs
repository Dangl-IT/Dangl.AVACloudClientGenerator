using Dangl.AVACloudClientGenerator.Shared;
using System;
using System.IO;
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
            var swaggerDocumentUri = string.IsNullOrWhiteSpace(_clientGeneratorOptions.SwaggerDocUri)
                ? Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT
                : _clientGeneratorOptions.SwaggerDocUri;
            switch (_clientGeneratorOptions.ClientLanguage)
            {
                case ClientLanguage.Java:
                    await GenerateJavaClient(swaggerDocumentUri);
                    break;

                case ClientLanguage.TypeScriptNode:
                    await GenerateTypeScriptNodeClient(swaggerDocumentUri);
                    break;

                default:
                    throw new NotImplementedException("The specified language is not supported");
            }

            await WriteClientCodeAsync();
        }

        private async Task GenerateJavaClient(string swaggerDocumentUri)
        {
            var javaOptionsGenerator = new JavaGenerator.OptionsGenerator(_avaCloudVersion);
            var javaGenerator = new JavaGenerator.CodeGenerator(javaOptionsGenerator, _avaCloudVersion);
            _zippedClientCodeStream = await javaGenerator.GetGeneratedCodeZipPackageAsync(swaggerDocumentUri);
        }

        private async Task GenerateTypeScriptNodeClient(string swaggerDocumentUri)
        {
            var typeScriptNodeOptionsGenerator = new TypeScriptNodeGenerator.OptionsGenerator(_avaCloudVersion);
            var typeScriptNodeGenerator = new TypeScriptNodeGenerator.CodeGenerator(typeScriptNodeOptionsGenerator, _avaCloudVersion);
            _zippedClientCodeStream = await typeScriptNodeGenerator.GetGeneratedCodeZipPackageAsync(swaggerDocumentUri);
        }

        private async Task WriteClientCodeAsync()
        {
            await new OutputWriter(_zippedClientCodeStream, _clientGeneratorOptions.OutputPathFolder)
                .WriteCodeToDirectoryAndAddReadmeAndLicense();
        }
    }
}
