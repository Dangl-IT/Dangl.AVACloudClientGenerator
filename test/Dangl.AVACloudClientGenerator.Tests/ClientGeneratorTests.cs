using Dangl.AVACloudClientGenerator.Shared;
using System.Threading.Tasks;
using Xunit;

namespace Dangl.AVACloudClientGenerator.Tests
{
    public class ClientGeneratorTests
    {
        private readonly AVACloudVersion _avaCloudVersion = new AVACloudVersion();

        [Fact]
        public async Task CanGenerateJavaClient()
        {
            var javaOptionsGenerator = new JavaGenerator.OptionsGenerator(_avaCloudVersion);
            var javaGenerator = new JavaGenerator.CodeGenerator(javaOptionsGenerator, _avaCloudVersion);
            using (var zippedClientCodeStream = await javaGenerator.GetGeneratedCodeZipPackageAsync())
            {
                Assert.NotNull(zippedClientCodeStream);
                Assert.True(zippedClientCodeStream.Length > 0);
            }
        }

        [Fact]
        public async Task CanGenerateTypeScriptNodeClient()
        {
            var typeScriptNodeOptionsGenerator = new TypeScriptNodeGenerator.OptionsGenerator(_avaCloudVersion);
            var typeScriptNodeGenerator = new TypeScriptNodeGenerator.CodeGenerator(typeScriptNodeOptionsGenerator, _avaCloudVersion);
            using (var zippedClientCodeStream = await typeScriptNodeGenerator.GetGeneratedCodeZipPackageAsync())
            {
                Assert.NotNull(zippedClientCodeStream);
                Assert.True(zippedClientCodeStream.Length > 0);
            }
        }
    }
}
