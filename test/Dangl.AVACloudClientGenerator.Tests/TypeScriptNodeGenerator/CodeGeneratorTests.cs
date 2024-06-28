using Dangl.AVACloudClientGenerator.Shared;
using System.Threading.Tasks;
using Xunit;

namespace Dangl.AVACloudClientGenerator.Tests.TypeScriptNodeGenerator
{
    public class CodeGeneratorTests
    {
        private readonly AVACloudVersion _avaCloudVersion = new AVACloudVersion();

        [Fact]
        public async Task CanGenerateTypeScriptFetchClient()
        {
            var typeScriptNodeGenerator = new Dangl.AVACloudClientGenerator.TypeScriptNodeGenerator.CodeGenerator(_avaCloudVersion);
            var zipStream = await typeScriptNodeGenerator.GetGeneratedCodeZipPackageAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT);
            Assert.True(zipStream.Length > 0);
        }
    }
}
