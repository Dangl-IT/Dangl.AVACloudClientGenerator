using Dangl.AVACloudClientGenerator.Shared;
using NSwag;
using NSwag.CodeGeneration.TypeScript;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.TypeScriptFetchGenerator
{
    public class CodeGenerator
    {
        private readonly AVACloudVersion _avaCloudVersion;

        public CodeGenerator(AVACloudVersion avaCloudVersion)
        {
            _avaCloudVersion = avaCloudVersion;
        }

        public async Task<Stream> GetGeneratedCodeZipPackageAsync(string swaggerDocumentUri)
        {
            var document = await OpenApiDocument.FromUrlAsync(swaggerDocumentUri);

            var settings = new TypeScriptClientGeneratorSettings
            {
                ClassName = "{controller}Client"
            };

            var generator = new TypeScriptClientGenerator(document, settings);
            var code = generator.GenerateFile();

            var memStream = new MemoryStream();
            using (var zipArchive = new ZipArchive(memStream, ZipArchiveMode.Create, true))
            {
                var clientEntry = zipArchive.CreateEntry("api.ts");
                using (var entryStream = clientEntry.Open())
                {
                    using var streamWriter = new StreamWriter(entryStream);
                    await streamWriter.WriteAsync(code);
                }

                await AddReadme(zipArchive);
                await AddGitignore(zipArchive);
                await AddTsConfig(zipArchive);
                await AddPackageJson(zipArchive);
            }

            memStream.Position = 0;
            return memStream;
        }

        private async Task AddReadme(ZipArchive zipArchive)
        {
            var entry = zipArchive.CreateEntry("README.md");
            using var entryStream = entry.Open();
            using var streamWriter = new StreamWriter(entryStream);
            var content = ReadmeFactory.GetReadmeContent();
            await streamWriter.WriteAsync(content);
        }

        private async Task AddGitignore(ZipArchive zipArchive)
        {
            var entry = zipArchive.CreateEntry(".gitignore");
            using var entryStream = entry.Open();
            using var streamWriter = new StreamWriter(entryStream);
            var content = $"node_modules{Environment.NewLine}";
            await streamWriter.WriteAsync(content);
        }

        private async Task AddTsConfig(ZipArchive zipArchive)
        {
            var entry = zipArchive.CreateEntry("tsconfig.json");
            using var entryStream = entry.Open();
            using var streamWriter = new StreamWriter(entryStream);
            var content = @"{
  ""compilerOptions"": {
    ""module"": ""commonjs"",
    ""noImplicitAny"": false,
    ""suppressImplicitAnyIndexErrors"": true,
    ""target"": ""ES6"",
    ""strict"": true,
    ""moduleResolution"": ""node"",
    ""removeComments"": false,
    ""sourceMap"": true,
    ""noLib"": false,
    ""declaration"": true,
    ""lib"": [""dom"", ""es6"", ""es5"", ""dom.iterable"", ""scripthost""]
  },
  ""exclude"": [""node_modules""]
}
";
            await streamWriter.WriteAsync(content);
        }

        private async Task AddPackageJson(ZipArchive zipArchive)
        {
            var entry = zipArchive.CreateEntry("package.json");
            using var entryStream = entry.Open();
            using var streamWriter = new StreamWriter(entryStream);
            var content = @"{
  ""name"": ""@dangl/avacloud-client-fetch"",
  ""version"": ""1.0.0"",
  ""description"": ""AVACloud client"",
  ""author"": ""Dangl IT GmbH"",
  ""main"": ""api.js"",
  ""types"": ""api.d.ts"",
  ""scripts"": {
    ""build"": ""tsc""
  },
  ""devDependencies"": {
    ""typescript"": ""^4.9.5""
  }
}
";
            await streamWriter.WriteAsync(content);
        }
    }
}
