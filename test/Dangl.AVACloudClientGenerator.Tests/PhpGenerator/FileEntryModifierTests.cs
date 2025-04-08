using Dangl.AVACloudClientGenerator.PhpGenerator;
using Dangl.AVACloudClientGenerator.Shared;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dangl.AVACloudClientGenerator.Tests.PhpGenerator
{
    public class FileEntryModifierTests
    {
        private readonly AVACloudVersion _avaCloudVersion = new AVACloudVersion();
        
        [Fact]
        public async Task SetsGuzzleToV744()
        {
            using var sourceStream = await GetSourceZipArchiveStreamAsync();
            var composerJsonContent = GetFileContentForArchive(sourceStream, "composer.json");

            Assert.Contains("\"guzzlehttp/guzzle\": \"^7.4.4\"", composerJsonContent);
            Assert.DoesNotContain("\"guzzlehttp/guzzle\": \"^6.2\"", composerJsonContent);
        }
        
        [Fact]
        public async Task UsesNewUtilityMethodsForFileLoading()
        {
            using var sourceStream = await GetSourceZipArchiveStreamAsync();
            var gaebConversionApiCode = GetFileContentForArchive(sourceStream, "GaebConversionApi.php");

            Assert.Contains("Utils::tryFopen", gaebConversionApiCode);
            Assert.DoesNotContain("try_fopen", gaebConversionApiCode);
        }

        private async Task<Stream> GetSourceZipArchiveStreamAsync()
        {
            var phpOptionsGenerator = new OptionsGenerator(_avaCloudVersion);
            var phpGenerator = new CodeGenerator(phpOptionsGenerator, _avaCloudVersion);
            return await phpGenerator.GetGeneratedCodeZipPackageAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT, "placeholder");
        }

        private string GetFileContentForArchive(Stream archiveStream, string entryName)
        {
            using (var zipArchive = new System.IO.Compression.ZipArchive(archiveStream))
            {
                var entry = zipArchive.Entries.Single(e => e.Name == entryName);
                using (var entryStream = entry.Open())
                {
                    using (var streamReader = new StreamReader(entryStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
        }
    }
}
