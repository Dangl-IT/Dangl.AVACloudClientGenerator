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

        [Option('s', "swaggergenendpoint", Required = false, HelpText = "Optional url to the swagger generator client gen endpoint. Will be ignored if using a local Docker generator.")]
        public string SwaggerGeneratorClientGenEndpoint { get; set; } = Constants.SWAGGER_GENERATOR_CLIENT_GEN_ENDPOINT;

        [Option('a', "openapigenendpoint", Required = false, HelpText = "Optional url to the OpenAPI generator client gen endpoint. Will be ignored if using a local Docker generator.")]
        public string OpenApiGeneratorClientGenEndpoint { get; set; } = Constants.OPENAPI_GENERATOR_CLIENT_GEN_ENDPOINT;

        [Option('d', "docker", Required = false, Default = false, HelpText = "Use local Docker containers for the generation. This is required for some languages.")]
        public bool UseLocalDockerContainers { get; set; }
    }
}
