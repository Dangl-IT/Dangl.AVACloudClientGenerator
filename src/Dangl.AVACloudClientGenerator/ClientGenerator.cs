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
            switch (_clientGeneratorOptions.ClientLanguage)
            {
                case ClientLanguage.Java:
                    await GenerateJavaClient();
                    break;

                case ClientLanguage.TypeScriptNode:
                    await GenerateTypeScriptNodeClient();
                    break;

                default:
                    throw new NotImplementedException("The specified language is not supported");
            }

            await WriteClientCodeAsync();
        }

        private async Task GenerateJavaClient()
        {
            var javaOptionsGenerator = new JavaGenerator.OptionsGenerator(_avaCloudVersion);
            var javaGenerator = new JavaGenerator.CodeGenerator(javaOptionsGenerator, _avaCloudVersion);
            _zippedClientCodeStream = await javaGenerator.GetGeneratedCodeZipPackageAsync();
        }

        private async Task GenerateTypeScriptNodeClient()
        {
            var typeScriptNodeOptionsGenerator = new TypeScriptNodeGenerator.OptionsGenerator(_avaCloudVersion);
            var typeScriptNodeGenerator = new TypeScriptNodeGenerator.CodeGenerator(typeScriptNodeOptionsGenerator, _avaCloudVersion);
            _zippedClientCodeStream = await typeScriptNodeGenerator.GetGeneratedCodeZipPackageAsync();
        }

        private async Task WriteClientCodeAsync()
        {
            await new OutputWriter(_zippedClientCodeStream, _clientGeneratorOptions.OutputPathFolder)
                .WriteCodeToDirectoryAndAddReadmeAndLicense();
        }
    }
}
