using Dangl.AVACloudClientGenerator.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.DartGenerator
{
    public class OptionsGenerator
    {
        private readonly AVACloudVersion _avaCloudVersion;

        public OptionsGenerator(AVACloudVersion avaCloudVersion)
        {
            _avaCloudVersion = avaCloudVersion;
        }

        public async Task<Dictionary<string, object>> GetPythonClientGeneratorOptionsAsync(string swaggerDocumentUri)
        {
            var avaCloudVersion = await _avaCloudVersion.GetAvaCloudVersionAsync(swaggerDocumentUri);
            return new Dictionary<string, object>
            {
                { "pubName", "avacloud-client-dart" },
                { "pubVersion", avaCloudVersion }
            };
        }
    }
}
