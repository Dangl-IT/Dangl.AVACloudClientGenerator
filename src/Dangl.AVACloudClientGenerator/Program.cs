using CommandLine;
using CommandLine.Text;
using System;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            HeadingInfo.Default.WriteMessage("Visit https://www.dangl-it.com to find out more about AVACloud");
            HeadingInfo.Default.WriteMessage("This generator is available on GitHub: https://github.com/Dangl-IT/Dangl.AVACloudClientGenerator");
            HeadingInfo.Default.WriteMessage("Version: " + VersionsService.Version);
            var parsedOptions = Parser.Default.ParseArguments<ClientGeneratorOptions>(args);
            if (parsedOptions.Tag == ParserResultType.Parsed)
            {
                var clientGeneratorOptions = (Parsed<ClientGeneratorOptions>)parsedOptions;

                var dockerContainerManager = new DockerContainerManager();
                var hasStartedDocker = false;
                try
                {
                    if (clientGeneratorOptions.Value.UseLocalDockerContainers || true)
                    {
                        Console.WriteLine("Starting Docker containers...");
                        var containerPorts = await dockerContainerManager.StartDockerContainersAsync();
                        clientGeneratorOptions.Value.SwaggerGeneratorClientGenEndpoint = $"http://localhost:{containerPorts.swaggerGenDockerContainerPort}/api/gen/clients/";
                        clientGeneratorOptions.Value.OpenApiGeneratorClientGenEndpoint = $"http://localhost:{containerPorts.openApiGenDockerContainerPort}/api/gen/clients/";
                        hasStartedDocker = true;
                    }

                    var generator = new ClientGenerator(clientGeneratorOptions.Value);
                    await generator.GenerateClientCodeAsync();
                    Console.WriteLine("Client Generated");
                }
                catch (Exception e)
                {
                    DisplayExceptionDetails(e);
                }
                finally
                {
                    if (hasStartedDocker)
                    {
                        await dockerContainerManager.StopDockerContainersAsync();
                    }
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
