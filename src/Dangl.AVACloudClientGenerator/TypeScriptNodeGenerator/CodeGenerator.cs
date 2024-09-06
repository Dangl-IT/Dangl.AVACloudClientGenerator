using Dangl.AVACloudClientGenerator.Shared;
using Dangl.AVACloudClientGenerator.Utilities;
using NSwag;
using NSwag.CodeGeneration.TypeScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dangl.AVACloudClientGenerator.TypeScriptNodeGenerator
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
                ClassName = "{controller}",
                WrapResponses = true,
            };
            settings.Template = TypeScriptTemplate.Fetch;
            var operationNameGenerator = new TypeScriptNodeClientOperationNameGenerator();
            settings.OperationNameGenerator = operationNameGenerator;
            settings.TypeScriptGeneratorSettings.GenerateConstructorInterface = true;
            settings.TypeScriptGeneratorSettings.ConvertConstructorInterfaceData = true;
            settings.TypeScriptGeneratorSettings.MarkOptionalProperties = true;

            var generator = new TypeScriptClientGenerator(document, settings);
            var code = generator.GenerateFile();

            var memStream = new MemoryStream();
            using (var zipArchive = new ZipArchive(memStream, ZipArchiveMode.Create, true))
            {
                var clientEntry = zipArchive.CreateEntry("api.ts");
                using (var entryStream = clientEntry.Open())
                {
                    code = code.GenerateMethodOverloadsWithOptionsObject();
                    code = UpdateCodeWithBackwardsCompatibilityFeatures(code, operationNameGenerator.GeneratedOperationNames.Distinct().ToList());
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

        private string UpdateCodeWithBackwardsCompatibilityFeatures(string code,
            List<string> operationNames)
        {
            // After we've switched from the old generator.swagger.io based one to NSwag, there were some
            // differences that we would like to keep backwards compatible. This method adds those features

            // 1. The 'basePath' property is now 'baseUrl', so we'll add a property that sets the baseUrl
            var lines = Regex.Split(code, @"\r\n?|\n");
            var updatedCode = string.Empty;
            foreach (var line in lines)
            {
                if (!line.Contains("private baseUrl: string;"))
                {
                    updatedCode += line + Environment.NewLine;
                    continue;
                }

                updatedCode += "    private baseUrl: string;" + Environment.NewLine;
                updatedCode += @"    public get basePath(): string {
        return this.baseUrl;
    }
    public set basePath(value: string) {
        this.baseUrl = value;
    }" + Environment.NewLine;
            }

            if (updatedCode == code)
            {
                throw new Exception("Failed to add the basePath property to the code. This is unexpected, likely the NSwag template has changed.");
            }

            code = updatedCode;

            // 2. The `accessToken` was present on clients, so we need to add it back
            // and also update the logic to use the tokens if present in the Authorization header
            lines = Regex.Split(code, @"\r\n?|\n");
            updatedCode = string.Empty;

            var isInOpenOptions = false;
            foreach (var line in lines)
            {
                if (line.Contains("private baseUrl: string;"))
                {
                    updatedCode += "    private _accessToken: string | null = null" + Environment.NewLine;
                    updatedCode += "    set accessToken(token: string | null) {" + Environment.NewLine;
                    updatedCode += "        this._accessToken = token;" + Environment.NewLine;
                    updatedCode += "    }" + Environment.NewLine;
                    updatedCode += line + Environment.NewLine;
                }
                else if (isInOpenOptions && line.Trim() == "};")
                {
                    updatedCode += line + Environment.NewLine;
                    isInOpenOptions = false;
                    updatedCode += Environment.NewLine;
                    updatedCode += "        if (this._accessToken) {" + Environment.NewLine;
                    updatedCode += "            options_.headers = options_.headers || {};" + Environment.NewLine;
                    updatedCode += "            options_.headers['Authorization'] = `Bearer ${this._accessToken}`;" + Environment.NewLine;
                    updatedCode += "        }" + Environment.NewLine;
                }
                else
                {
                    updatedCode += line + Environment.NewLine;
                    if (!isInOpenOptions && line.Trim().StartsWith("let options_:"))
                    {
                        isInOpenOptions = true;
                    }
                }
            }

            if (updatedCode == code)
            {
                throw new Exception("Failed to add the accessToken property to the code. This is unexpected, likely the NSwag template has changed.");
            }

            code = updatedCode;

            // 3. All of the arguments for the conversions are required, but we want to keep them optional
            // and just use 'undefined' in such cases
            lines = Regex.Split(code, @"\r\n?|\n");
            updatedCode = string.Empty;
            var operationNamesRegex = operationNames
                .Select(on => on[0].ToString().ToLowerInvariant() + on.Substring(1))
                .Aggregate((c, n) => $"{c}|{n}");
            foreach (var line in lines)
            {
                var isMethodDeclaration = Regex.IsMatch(line, @$"^\s*({operationNamesRegex})\(");
                if (!isMethodDeclaration)
                {
                    updatedCode+=line + Environment.NewLine;
                    continue;
                }

                var parameters = Regex.Match(line, @"\((.*)\)").Groups[1].Value;
                if (string.IsNullOrWhiteSpace(parameters))
                {
                    updatedCode += line + Environment.NewLine;
                    continue;
                }

                var splitParameters = parameters.Split(',');
                var updatedParameters = string.Empty;
                var isFirst = true;
                foreach (var parameter in splitParameters)
                {
                    if (parameter.Contains("| undefined") && !parameter.Contains("File:") && !isFirst)
                    {
                        updatedParameters += parameter.Replace(":", "?:");
                    }
                    else
                    {
                        updatedParameters += parameter;
                    }

                    isFirst = false;
                    updatedParameters += ",";
                }
                updatedCode+=line.Replace(parameters, updatedParameters.TrimEnd(',')) + Environment.NewLine;
            }

            updatedCode = updatedCode.TrimEnd();

            if (updatedCode == code)
            {
                throw new Exception("Failed to make the method parameters optional. This is unexpected, likely the NSwag template has changed.");
            }

            code = updatedCode;

            // 4. We also need to add the 'elementTypeDiscriminator' back to IElementDto
            lines = Regex.Split(code, @"\r\n?|\n");
            updatedCode = string.Empty;
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("export abstract class IElementDto"))
                {
                    updatedCode += line + Environment.NewLine;
                    updatedCode += "    elementType?: string;" + Environment.NewLine;
                    updatedCode += "    elementTypeDiscriminator?: string;" + Environment.NewLine;
                }
                else
                {
                    updatedCode += line + Environment.NewLine;
                }
            }

            updatedCode = updatedCode.TrimEnd();

            if (updatedCode == code)
            {
                throw new Exception("Failed to add the elementTypeDiscriminator property to the code. This is unexpected, likely the NSwag template has changed.");
            }

            code = updatedCode;

            // 5. We need to provide a default value for 'fetch' if nothing is given
            updatedCode = code.Replace("this.http = http ? http : window as any;", "this.http = http ? http : (typeof window !== 'undefined' ? window : { fetch });");
            if (updatedCode == code)
            {
                throw new Exception("Failed to add the default fetch implementation to the code. This is unexpected, likely the NSwag template has changed.");
            }

            code = updatedCode;

            // 6. Now we also want to update all the inner calls from `toJSON` to use the plain object
            // in case the `toJSON` method isn't present, e.g. when working with plain objects
            // instead of actual `Dto` instances
            lines = Regex.Split(code, @"\r\n?|\n");
            updatedCode = string.Empty;
            var toJsonPushRegex = new Regex(@"(\s*)data\[""([a-zA-Z0-9]+)""\]\.push\(item\.toJSON\(\)\);");
            foreach (var line in lines)
            {
                if (toJsonPushRegex.IsMatch(line))
                {
                    var indention = toJsonPushRegex.Match(line).Groups.Values.Skip(1).First();
                    var propertyName = toJsonPushRegex.Match(line).Groups.Values.Last();

                    updatedCode += indention + "if (typeof item.toJSON !== \"undefined\") {" + Environment.NewLine;
                    updatedCode += "  " + line + Environment.NewLine;
                    updatedCode += indention + "} else {" + Environment.NewLine;
                    updatedCode += indention + " data[\"" + propertyName + "\"].push(item);" + Environment.NewLine;
                    updatedCode += indention + "}" + Environment.NewLine;
                }
                else
                {
                    updatedCode += line + Environment.NewLine;
                }
            }
            code = updatedCode;

            // 7. There are also other calls to `toJSON` that are not in array mapping methods
            lines = Regex.Split(code, @"\r\n?|\n");
            updatedCode = string.Empty;
            var toJsonRegex = new Regex(@"(\s*)data\[""([a-zA-Z0-9]+)""\] = this\.([a-zA-Z0-9]+) \? this.([a-zA-Z0-9]+)\.toJSON\(\) : <any>undefined;");
            foreach (var line in lines)
            {
                var match = toJsonRegex.Match(line);
                if (match.Success && match.Groups.Count == 5
                    && match.Groups[2].Value == match.Groups[3].Value
                    && match.Groups[3].Value == match.Groups[4].Value)
                {
                    var indention = match.Groups[1].Value;
                    var propertyName = match.Groups[2].Value;

                    updatedCode += indention + $"if (this.{propertyName}) {{" + Environment.NewLine;
                    updatedCode += indention + $"  if (typeof this.{propertyName}.toJSON !== \"undefined\") {{" + Environment.NewLine;
                    updatedCode += indention + $"  data[\"{propertyName}\"] = this.{propertyName}.toJSON();" + Environment.NewLine;
                    updatedCode += indention + "  } else {" + Environment.NewLine;
                    updatedCode += indention + $"  data[\"{propertyName}\"] = this.{propertyName};" + Environment.NewLine;
                    updatedCode += indention + "  }" + Environment.NewLine;
                    updatedCode += indention + "} else {" + Environment.NewLine;
                    updatedCode += indention + $"  data[\"{propertyName}\"] = <any>undefined;" + Environment.NewLine;
                    updatedCode += indention + "}" + Environment.NewLine;
                }
                else
                {
                    updatedCode += line + Environment.NewLine;
                }
            }
            code = updatedCode;

            // 8. For compatibility reasons between the itnerfaces and actual class implementations, we also
            // want to `init` and `toJSON` properties to be optional
            lines = Regex.Split(code, @"\r\n?|\n");
            updatedCode = string.Empty;
            foreach (var line in lines)
            {
                if (line.Trim() == "toJSON(data?: any) {")
                {
                    updatedCode += line.Replace("toJSON(", "toJSON?(") + Environment.NewLine;
                }
                else if (line.Trim() == "init(_data?: any) {")
                {
                    updatedCode += line.Replace("init(", "init?(") + Environment.NewLine;
                }
                else if (line.Trim() == "result.init(data);")
                {
                    updatedCode += line.Replace("init(", "init!(") + Environment.NewLine;
                }
                else if (line.Trim() == "super.init(_data);")
                {
                    updatedCode += "    if (typeof super.init !== \"undefined\") {" + Environment.NewLine;
                    updatedCode += line + Environment.NewLine;
                    updatedCode += "    }" + Environment.NewLine;
                }
                else if (line.Trim() == "super.toJSON(data);")
                {
                    updatedCode += "    if (typeof super.toJSON !== \"undefined\") {" + Environment.NewLine;
                    updatedCode += line + Environment.NewLine;
                    updatedCode += "    }" + Environment.NewLine;
                }
                else
                {
                    updatedCode += line + Environment.NewLine;
                }
            }
            code = updatedCode;

            return code;
        }

        private async Task AddReadme(ZipArchive zipArchive)
        {
            var entry = zipArchive.CreateEntry("README.md");
            using var entryStream = entry.Open();
            using var streamWriter = new StreamWriter(entryStream);
            var packageReadmeDetails = @"## Update Notice

Starting with version **1.30.0**, the package dropped external dependencies and did some refactoring to the code. The following changes are required:

* `FileParameter` now uses `data` instead of `value` for the file content
* `FileParameter` now uses `fileName` instead of `options.filename` for the file name
* All responses are now wrapped in a response object, making it easier to access e.g. returned headers or status codes
* File responses now return a `Blob` instead of an `Buffer`, and are accessible via `response.result` instead of `response.body`

";
            var content = ReadmeFactory.GetReadmeContent(packageDetails: packageReadmeDetails);
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
  ""name"": ""@dangl/avacloud-client-node"",
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
