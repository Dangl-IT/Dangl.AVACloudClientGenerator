using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.Shared
{
    public class AVACloudVersion
    {
        private string _avaCloudversion;

        public async Task<string> GetAvaCloudVersionAsync()
        {
            if (!string.IsNullOrWhiteSpace(_avaCloudversion))
            {
                return _avaCloudversion;
            }

            var httpClient = new HttpClient();
            var swaggerDefinition = await httpClient
                .GetStringAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT);
            var json = JObject.Parse(swaggerDefinition);
            var avacloudVersion = (string)json["info"]["version"];
            _avaCloudversion = avacloudVersion;
            return avacloudVersion;
        }
    }
}
