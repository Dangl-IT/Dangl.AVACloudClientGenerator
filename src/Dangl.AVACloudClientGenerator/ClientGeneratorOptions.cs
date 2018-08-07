using CommandLine;

namespace Dangl.AVACloudClientGenerator
{
    public class ClientGeneratorOptions
    {
        [Option('o', "output", Required = true, HelpText = "Relative or absolute path to the folder where the generated client should be saved")]
        public string OutputPathFolder { get; set; }

        [Option('l', "language", Required = true, HelpText = "The language of the client to generate")]
        public ClientLanguage ClientLanguage { get; set; }
    }
}
