using Dangl.AVACloudClientGenerator.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.TypeScriptNodeGenerator
{
    public class OptionsGenerator
    {
        private readonly AVACloudVersion _avaCloudVersion;

        public OptionsGenerator(AVACloudVersion avaCloudVersion)
        {
            _avaCloudVersion = avaCloudVersion;
        }

        public async Task<Dictionary<string, object>> GetTypescriptNodeClientGeneratorOptionsAsync(string swaggerDocumentUri)
        {
            var avaCloudVersion = await _avaCloudVersion.GetAvaCloudVersionAsync(swaggerDocumentUri);
            return new Dictionary<string, object>
            {
                { "supportsES6", "true" },
                { "npmName", "@dangl/avacloud-client-node" },
                { "npmVersion", avaCloudVersion }
            };
        }
    }
}
