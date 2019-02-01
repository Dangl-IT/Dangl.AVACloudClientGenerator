using Dangl.AVACloudClientGenerator.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.JavaScriptGenerator
{
    public class OptionsGenerator
    {
        private readonly AVACloudVersion _avaCloudVersion;

        public OptionsGenerator(AVACloudVersion avaCloudVersion)
        {
            _avaCloudVersion = avaCloudVersion;
        }

        public async Task<Dictionary<string, object>> GetJavaScriptClientGeneratorOptionsAsync(string swaggerDocumentUri)
        {
            var avaCloudVersion = await _avaCloudVersion.GetAvaCloudVersionAsync(swaggerDocumentUri);
            return new Dictionary<string, object>
            {
                { "projectName", "@dangl/avacloud-client-javascript" },
                { "moduleName", "DanglAVACloudClient" }
            };
        }
    }
}
