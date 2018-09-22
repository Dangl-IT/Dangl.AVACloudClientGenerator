using CommandLine;

namespace Dangl.AVACloudClientGenerator
{
    public class ClientGeneratorOptions
    {
        [Option('o', "output", Required = true, HelpText = "Relative or absolute path to the folder where the generated client should be saved")]
        public string OutputPathFolder { get; set; }

        [Option('l', "language", Required = true, HelpText = "The language of the client to generate")]
        public ClientLanguage ClientLanguage { get; set; }

        [Option('u', "uri", Required = false, Default = Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT, HelpText = "Optional url to the swagger document, defaults to " + Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT)]
        public string SwaggerDocUri { get; set; }
    }
}
