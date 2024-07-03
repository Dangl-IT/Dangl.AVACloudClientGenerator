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

        public async Task<Dictionary<string, object>> GetDartClientGeneratorOptionsAsync(string swaggerDocumentUri)
        {
            var avaCloudVersion = await _avaCloudVersion.GetAvaCloudVersionAsync(swaggerDocumentUri);
            return new Dictionary<string, object>
            {
                { "pubAuthorEmail", "info@dangl-it.com" },
                { "pubAuthor", "Dangl IT GmbH" },
                { "pubLibrary", "avacloud_client_dart" },
                { "pubName", "avacloud_client_dart" },
                { "pubVersion", avaCloudVersion }
            };
        }
    }
}
