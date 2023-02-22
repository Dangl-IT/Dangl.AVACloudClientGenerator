using Dangl.AVACloudClientGenerator.Shared;
using System.Threading.Tasks;
using Xunit;

namespace Dangl.AVACloudClientGenerator.Tests.TypeScriptFetchGenerator
{
    public class CodeGeneratorTests
    {
        private readonly AVACloudVersion _avaCloudVersion = new AVACloudVersion();

        [Fact]
        public async Task CanGenerateTypeScriptFetchClient()
        {
            var typeScriptNodeGenerator = new Dangl.AVACloudClientGenerator.TypeScriptFetchGenerator.CodeGenerator(_avaCloudVersion);
            var zipStream = await typeScriptNodeGenerator.GetGeneratedCodeZipPackageAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT);
            Assert.True(zipStream.Length > 0);
        }
    }
}
