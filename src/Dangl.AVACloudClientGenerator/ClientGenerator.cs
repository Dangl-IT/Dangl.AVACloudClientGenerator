using Dangl.AVACloudClientGenerator.Shared;
using System;
using System.IO;
using System.IO.Compression;
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

        private async Task WriteClientCodeAsync()
        {
            using (var zipArchive = new System.IO.Compression.ZipArchive(_zippedClientCodeStream))
            {
                foreach (var entry in zipArchive.Entries)
                {
                    await WriteSingleEntryAsync(entry);
                }
            }
        }

        private async Task WriteSingleEntryAsync(ZipArchiveEntry entry)
        {
            var filePath = GetFilePath(entry);
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            using (var entryStream = entry.Open())
            {
                using (var outputStream = File.Create(filePath))
                {
                    await entryStream.CopyToAsync(outputStream);
                }
            }
        }

        private string GetFilePath(ZipArchiveEntry entry)
        {
            return Path.Combine(_clientGeneratorOptions.OutputPathFolder, entry.FullName);
        }
    }
}
