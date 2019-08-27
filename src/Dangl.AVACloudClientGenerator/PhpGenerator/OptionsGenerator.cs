using Dangl.AVACloudClientGenerator.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.PhpGenerator
{
    public class OptionsGenerator
    {
        private readonly AVACloudVersion _avaCloudVersion;

        public OptionsGenerator(AVACloudVersion avaCloudVersion)
        {
            _avaCloudVersion = avaCloudVersion;
        }

        public async Task<Dictionary<string, object>> GetPhpClientGeneratorOptionsAsync(string swaggerDocumentUri)
        {
            var avaCloudVersion = await _avaCloudVersion.GetAvaCloudVersionAsync(swaggerDocumentUri);
            return new Dictionary<string, object>
            {
                { "packagePath", "Dangl\\AVACloud" },
                { "composerVendorName", "dangl" },
                { "composerProjectName", "avacloud" },
                { "artifactVersion", avaCloudVersion},
                { "gitUserId", "GeorgDangl" },
                { "gitRepoId", "AVACloud" }
            };
        }
    }
}
