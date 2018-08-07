using Dangl.AVACloudClientGenerator.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.JavaGenerator
{
    public class OptionsGenerator
    {
        private readonly AVACloudVersion _avaCloudVersion;

        public OptionsGenerator(AVACloudVersion avaCloudVersion)
        {
            _avaCloudVersion = avaCloudVersion;
        }

        public async Task<Dictionary<string, object>> GetJavaClientGeneratorOptionsAsync()
        {
            var avaCloudVersion = await _avaCloudVersion.GetAvaCloudVersionAsync();
            return new Dictionary<string, object>
            {
                { "modelPackage", "com.danglit.avacloud.client.models" },
                { "apiPackage", "com.danglit.avacloud.client.api" },
                { "invokerPackage", "com.danglit.avacloud.client.invoker" },
                { "groupId", "com.danglit.avacloud.client" },
                { "artifactId", "com.danglit.avacloud.client" },
                { "artifactVersion", avaCloudVersion },
                { "artifactUrl", "https://www.dangl-it.com" },
                { "artifactDescription", "AVACloud Java Client - GAEB & AVA as a Service" },
                { "developerName", "Dangl IT" },
                { "developerEmail", "info@dangl-it.com" },
                { "java8", "true" },
                { "useGzipFeature", false },
                { "dateLibrary", "java8" }
            };
        }
    }
}
