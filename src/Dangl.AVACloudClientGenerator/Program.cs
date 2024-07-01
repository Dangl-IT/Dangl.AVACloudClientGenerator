using CommandLine;
using CommandLine.Text;
using System;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            HeadingInfo.Default.WriteMessage("Visit https://www.dangl-it.com to find out more about AVACloud");
            HeadingInfo.Default.WriteMessage("This generator is available on GitHub: https://github.com/Dangl-IT/Dangl.AVACloudClientGenerator");
            HeadingInfo.Default.WriteMessage("Version: " + VersionsService.Version);
            var parsedOptions = Parser.Default.ParseArguments<ClientGeneratorOptions>(args);
            if (parsedOptions.Tag == ParserResultType.Parsed)
            {
                var clientGeneratorOptions = (Parsed<ClientGeneratorOptions>)parsedOptions;
                try
                {
                    using (var generator = new ClientGenerator(clientGeneratorOptions.Value))
                    {
                        await generator.GenerateClientCodeAsync();
                    }
                    Console.WriteLine("Client Generated");
                }
                catch (Exception e)
                {
                    DisplayExceptionDetails(e);
                }
            }
        }

        private static void DisplayExceptionDetails(Exception e)
        {
            Console.Write(e.ToString());
            Console.WriteLine();
        }
    }
}
