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
            using (var zippedClientCodeStream = await javaGenerator.GetGeneratedCodeZipPackageAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT))
            {
                Assert.NotNull(zippedClientCodeStream);
                Assert.True(zippedClientCodeStream.Length > 0);
            }
        }

        [Fact]
        public async Task CanGenerateTypeScriptNodeClient()
        {
            var typeScriptNodeGenerator = new AVACloudClientGenerator.TypeScriptNodeGenerator.CodeGenerator(_avaCloudVersion);
            using (var zippedClientCodeStream = await typeScriptNodeGenerator.GetGeneratedCodeZipPackageAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT))
            {
                Assert.NotNull(zippedClientCodeStream);
                Assert.True(zippedClientCodeStream.Length > 0);
            }
        }

        [Fact(Skip = "This is currently running into a timeout, so we're ignoring this test. Something seems to be wrong at generator.swagger.io")]
        public async Task CanGenerateJavaScriptClient()
        {
            var javaScriptOptionsGenerator = new Dangl.AVACloudClientGenerator.JavaScriptGenerator.OptionsGenerator(_avaCloudVersion);
            var javaScriptGenerator = new Dangl.AVACloudClientGenerator.JavaScriptGenerator.CodeGenerator(javaScriptOptionsGenerator, _avaCloudVersion);
            using (var zippedClientCodeStream = await javaScriptGenerator.GetGeneratedCodeZipPackageAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT))
            {
                Assert.NotNull(zippedClientCodeStream);
                Assert.True(zippedClientCodeStream.Length > 0);
            }
        }

        [Fact]
        public async Task CanGeneratePhpClient()
        {
            var phpOptionsGenerator = new Dangl.AVACloudClientGenerator.PhpGenerator.OptionsGenerator(_avaCloudVersion);
            var phpGenerator = new Dangl.AVACloudClientGenerator.PhpGenerator.CodeGenerator(phpOptionsGenerator, _avaCloudVersion);
            using (var zippedClientCodeStream = await phpGenerator.GetGeneratedCodeZipPackageAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT))
            {
                Assert.NotNull(zippedClientCodeStream);
                Assert.True(zippedClientCodeStream.Length > 0);
            }
        }

        [Fact]
        public async Task CanGeneratePythonClient()
        {
            var pythonOptionsGenerator = new Dangl.AVACloudClientGenerator.PythonGenerator.OptionsGenerator(_avaCloudVersion);
            var pythonGenerator = new Dangl.AVACloudClientGenerator.PythonGenerator.CodeGenerator(pythonOptionsGenerator, _avaCloudVersion);
            using (var zippedClientCodeStream = await pythonGenerator.GetGeneratedCodeZipPackageAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT))
            {
                Assert.NotNull(zippedClientCodeStream);
                Assert.True(zippedClientCodeStream.Length > 0);
            }
        }
    }
}
