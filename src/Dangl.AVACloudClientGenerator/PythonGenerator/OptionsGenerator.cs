using Dangl.AVACloudClientGenerator.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.PythonGenerator
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
                { "packageName", "avacloud_client_python" },
                { "projectName", "avacloud-client-python" },
                { "packageVersion", avaCloudVersion },
                { "packageUrl", "" } // TODO GITHUB URL
            };
        }
    }
}
