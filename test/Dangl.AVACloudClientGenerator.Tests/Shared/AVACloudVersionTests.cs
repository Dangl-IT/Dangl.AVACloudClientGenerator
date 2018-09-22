using Dangl.AVACloudClientGenerator.Shared;
using System.Threading.Tasks;
using Xunit;

namespace Dangl.AVACloudClientGenerator.Tests.Shared
{
    public class AVACloudVersionTests
    {
        [Fact]
        public async Task CanGetVersion()
        {
            var version = await new AVACloudVersion().GetAvaCloudVersionAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT);
            Assert.False(string.IsNullOrWhiteSpace(version));
        }
    }
}
