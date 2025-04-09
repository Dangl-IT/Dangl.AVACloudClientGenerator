using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator
{
    public class DockerContainerManager
    {
        private DockerClient _cachedClient;
        private string _swaggerGenDockerContainerId;
        private string _openApiGenDockerContainerId;

        public async Task<(int swaggerGenDockerContainerPort, int openApiGenDockerContainerPort)> StartDockerContainersAsync()
        {
            var swaggerGenDockerContainerPort = await StartSwaggerGenDockerContainerAsync();
            var openApiGenDockerContainerPort = await StartOpenApiGenDockerContainerAsync();
            return (swaggerGenDockerContainerPort, openApiGenDockerContainerPort);
        }

        public async Task StopDockerContainersAsync()
        {
            var dockerClient = GetDockerClient();
            await dockerClient.Containers
                .StopContainerAsync(_swaggerGenDockerContainerId, new ContainerStopParameters());
            await dockerClient.Containers
                .StopContainerAsync(_openApiGenDockerContainerId, new ContainerStopParameters());
        }

        private async Task<int> StartSwaggerGenDockerContainerAsync()
        {
            var dockerClient = GetDockerClient();
            var freePort = GetFreePort();


            // We're pulling the latest Docker image
            try
            {
                await dockerClient.Images.CreateImageAsync(new ImagesCreateParameters
                {
                    FromImage = "swaggerapi/swagger-generator:latest"
                }, null, new Progress<JSONMessage>());
            }
            catch
            {
                // I'm not sure why, but the manual pull seemed to fail in some instances - the automatic download then worked, however.
            }

            var containerName = "AVACloudClientGen_" + Guid.NewGuid().ToString().Replace("-", string.Empty);
            var sqlContainerStartParameters = new CreateContainerParameters
            {
                Name = containerName,
                Image = "swaggerapi/swagger-generator:latest",
                Env = new List<string>
                    {
                        $"GENERATOR_HOST=http://localhost:{freePort}"
                    },
                HostConfig = new HostConfig
                {
                    AutoRemove = true,
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            {
                                "8080/tcp",
                                new PortBinding[]
                                {
                                    new PortBinding
                                    {
                                        HostPort = freePort.ToString()
                                    }
                                }
                            }
                        }
                }
            };

            var swaggerClientGeneratorDockerContainer = await dockerClient
                .Containers
                .CreateContainerAsync(sqlContainerStartParameters);

            await dockerClient
                .Containers
                .StartContainerAsync(swaggerClientGeneratorDockerContainer.ID, new ContainerStartParameters());

            await WaitUntilWebserviceIsAvailableAsync(freePort);
            _swaggerGenDockerContainerId = swaggerClientGeneratorDockerContainer.ID;
            return freePort;
        }

        private async Task<int> StartOpenApiGenDockerContainerAsync()
        {
            var dockerClient = GetDockerClient();
            var freePort = GetFreePort();

            // We're pulling the latest Docker image
            try
            {
                await dockerClient.Images.CreateImageAsync(new ImagesCreateParameters
                {
                    FromImage = "openapitools/openapi-generator-online:latest"
                }, null, new Progress<JSONMessage>());
            }
            catch
            {
                // I'm not sure why, but the manual pull seemed to fail in some instances - the automatic download then worked, however.
            }

            var containerName = "AVACloudClientGen_" + Guid.NewGuid().ToString().Replace("-", string.Empty);
            var sqlContainerStartParameters = new CreateContainerParameters
            {
                Name = containerName,
                Image = "openapitools/openapi-generator-online:latest",
                Env = new List<string>
                    {
                        $"GENERATOR_HOST=http://localhost:{freePort}"
                    },
                HostConfig = new HostConfig
                {
                    AutoRemove = true,
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            {
                                "8080/tcp",
                                new PortBinding[]
                                {
                                    new PortBinding
                                    {
                                        HostPort = freePort.ToString()
                                    }
                                }
                            }
                        }
                }
            };

            var swaggerClientGeneratorDockerContainer = await dockerClient
                .Containers
                .CreateContainerAsync(sqlContainerStartParameters);

            await dockerClient
                .Containers
                .StartContainerAsync(swaggerClientGeneratorDockerContainer.ID, new ContainerStartParameters());

            await WaitUntilWebserviceIsAvailableAsync(freePort);
            _openApiGenDockerContainerId = swaggerClientGeneratorDockerContainer.ID;
            return freePort;
        }

        private async Task WaitUntilWebserviceIsAvailableAsync(int port)
        {
            var timeoutInSeconds = 180;
            var responseReceived = false;
            var start = DateTime.UtcNow;
            var httpClient = new HttpClient();
            while (!responseReceived && DateTime.UtcNow < start.AddSeconds(timeoutInSeconds))
            {
                try
                {
                    responseReceived = (await httpClient.GetAsync($"http://localhost:{port}")).IsSuccessStatusCode;
                }
                catch
                {
                    await Task.Delay(1000);
                }
            }

            if (!responseReceived)
            {
                throw new Exception("Failed to start the Docker container, could not get a response in time.");
            }
        }

        private DockerClient GetDockerClient()
        {
            if (_cachedClient != null)
            {
                return _cachedClient;
            }

            var endpoint = new Uri(IsRunningOnWindows() ? "npipe://./pipe/docker_engine" : "unix:///var/run/docker.sock");
            var namedPipeConnectTimeout = TimeSpan.FromSeconds(5L);
            _cachedClient = new DockerClientConfiguration(endpoint, null, default(TimeSpan), namedPipeConnectTimeout).CreateClient();
            return _cachedClient;
        }

        private int GetFreePort()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }

        private bool IsRunningOnWindows()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }
    }
}
