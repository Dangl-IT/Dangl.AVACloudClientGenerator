namespace Dangl.AVACloudClientGenerator.Tests
{
    public static class DockerTestHelper
    {
        private static int _readerCount = 0;
        private static object _readerLock = new object();
        private static string _swaggerGenEndpoint;
        private static string _openApiGenEndpoint;

        private static DockerContainerManager _dockerContainerManager = new DockerContainerManager();

        public static string GetSwaggerDockerUrl()
        {
            lock (_readerLock)
            {
                _readerCount++;
                if (_readerCount == 1)
                {
                    var startResult = _dockerContainerManager.StartDockerContainersAsync()
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
                    _swaggerGenEndpoint = $"http://localhost:{startResult.swaggerGenDockerContainerPort}/api/gen/clients/";
                    _openApiGenEndpoint = $"http://localhost:{startResult.openApiGenDockerContainerPort}/api/gen/clients/";
                }
            }

            return _swaggerGenEndpoint;
        }

        public static string GetOpenApiDockerUrl()
        {
            lock (_readerLock)
            {
                _readerCount++;
                if (_readerCount == 1)
                {
                    var startResult = _dockerContainerManager.StartDockerContainersAsync()
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
                    _swaggerGenEndpoint = $"http://localhost:{startResult.swaggerGenDockerContainerPort}/api/gen/clients/";
                    _openApiGenEndpoint = $"http://localhost:{startResult.openApiGenDockerContainerPort}/api/gen/clients/";
                }
            }
            return _openApiGenEndpoint;
        }

        public static void DecrementReaderCount()
        {
            // Phew is this hacky, and unreliable, but it's good enough for GitHub Actions one-off runs
            lock (_readerLock)
            {
                _readerCount--;
                if (_readerCount == 0)
                {
                    _dockerContainerManager.StopDockerContainersAsync()
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
                }
            }
        }
    }
}
