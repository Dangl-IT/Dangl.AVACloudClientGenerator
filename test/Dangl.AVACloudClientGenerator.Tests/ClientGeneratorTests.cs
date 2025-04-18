﻿using Dangl.AVACloudClientGenerator.Shared;
using System.Threading.Tasks;
using Xunit;

namespace Dangl.AVACloudClientGenerator.Tests
{
    public static class ClientGeneratorTests
    {
        static readonly AVACloudVersion _avaCloudVersion = new AVACloudVersion();

        public class Java
        {
            [Fact]
            public async Task CanGenerateJavaClient()
            {
                try
                {
                    var javaOptionsGenerator = new JavaGenerator.OptionsGenerator(_avaCloudVersion);
                    var javaGenerator = new JavaGenerator.CodeGenerator(javaOptionsGenerator, _avaCloudVersion);
                    using (var zippedClientCodeStream = await javaGenerator.GetGeneratedCodeZipPackageAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT, DockerTestHelper.GetSwaggerDockerUrl()))
                    {
                        Assert.NotNull(zippedClientCodeStream);
                        Assert.True(zippedClientCodeStream.Length > 0);
                    }
                }
                finally
                {
                    DockerTestHelper.DecrementReaderCount();
                }
            }
        }

        public class TypeScriptNode
        {
            [Fact]
            public async Task CanGenerateTypeScriptNodeClient()
            {
                var typeScriptNodeGenerator = new AVACloudClientGenerator.TypeScriptNodeGenerator.CodeGenerator(_avaCloudVersion);
                using (var zippedClientCodeStream = await typeScriptNodeGenerator.GetGeneratedCodeZipPackageAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT))
                {
                    Assert.NotNull(zippedClientCodeStream);
                    Assert.True(zippedClientCodeStream.Length > 0);
                }
            }
        }

        public class JavaScript
        {
            [Fact(Skip = "This is currently running into a timeout, so we're ignoring this test. Something seems to be wrong at generator.swagger.io")]
            public async Task CanGenerateJavaScriptClient()
            {
                try
                {
                    var javaScriptOptionsGenerator = new AVACloudClientGenerator.JavaScriptGenerator.OptionsGenerator(_avaCloudVersion);
                    var javaScriptGenerator = new AVACloudClientGenerator.JavaScriptGenerator.CodeGenerator(javaScriptOptionsGenerator, _avaCloudVersion);
                    using (var zippedClientCodeStream = await javaScriptGenerator.GetGeneratedCodeZipPackageAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT, DockerTestHelper.GetSwaggerDockerUrl()))
                    {
                        Assert.NotNull(zippedClientCodeStream);
                        Assert.True(zippedClientCodeStream.Length > 0);
                    }
                }
                finally
                {
                    DockerTestHelper.DecrementReaderCount();
                }
            }
        }

        public class Php
        {
            [Fact]
            public async Task CanGeneratePhpClient()
            {
                try
                {
                    var phpOptionsGenerator = new AVACloudClientGenerator.PhpGenerator.OptionsGenerator(_avaCloudVersion);
                    var phpGenerator = new AVACloudClientGenerator.PhpGenerator.CodeGenerator(phpOptionsGenerator, _avaCloudVersion);
                    using (var zippedClientCodeStream = await phpGenerator.GetGeneratedCodeZipPackageAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT, DockerTestHelper.GetSwaggerDockerUrl()))
                    {
                        Assert.NotNull(zippedClientCodeStream);
                        Assert.True(zippedClientCodeStream.Length > 0);
                    }
                }
                finally
                {
                    DockerTestHelper.DecrementReaderCount();
                }
            }
        }

        public class Python
        {
            [Fact]
            public async Task CanGeneratePythonClient()
            {
                try
                {
                    var pythonOptionsGenerator = new PythonGenerator.OptionsGenerator(_avaCloudVersion);
                    var pythonGenerator = new PythonGenerator.CodeGenerator(pythonOptionsGenerator, _avaCloudVersion);
                    using (var zippedClientCodeStream = await pythonGenerator.GetGeneratedCodeZipPackageAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT, DockerTestHelper.GetSwaggerDockerUrl()))
                    {
                        Assert.NotNull(zippedClientCodeStream);
                        Assert.True(zippedClientCodeStream.Length > 0);
                    }
                }
                finally
                {
                    DockerTestHelper.DecrementReaderCount();
                }
            }
        }

        public class Dart
        {
            [Fact]
            public async Task CanGenerateDartClient()
            {
                try
                {
                    var dartOptionsGenerator = new DartGenerator.OptionsGenerator(_avaCloudVersion);
                    var dartGenerator = new DartGenerator.CodeGenerator(dartOptionsGenerator, _avaCloudVersion);
                    using (var zippedClientCodeStream = await dartGenerator.GetGeneratedCodeZipPackageAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT, DockerTestHelper.GetOpenApiDockerUrl()))
                    {
                        Assert.NotNull(zippedClientCodeStream);
                        Assert.True(zippedClientCodeStream.Length > 0);
                    }
                }
                finally
                {
                    DockerTestHelper.DecrementReaderCount();
                }
            }
        }
    }
}
