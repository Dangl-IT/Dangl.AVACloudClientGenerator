using Newtonsoft.Json.Linq;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.AzureKeyVault.Attributes;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Nuke.GitHub;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.IO.TextTasks;
using static Nuke.Common.Tooling.ProcessTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Git.GitTasks;
using static Nuke.Common.Tools.Npm.NpmTasks;
using static Nuke.GitHub.GitHubTasks;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.GenerateAndPublishPythonClient);

    [KeyVaultSettings(
        BaseUrlParameterName = nameof(KeyVaultBaseUrl),
        ClientIdParameterName = nameof(KeyVaultClientId),
        ClientSecretParameterName = nameof(KeyVaultClientSecret))]
    readonly KeyVaultSettings KeyVaultSettings;

    [Parameter] readonly string KeyVaultBaseUrl;
    [Parameter] readonly string KeyVaultClientId;
    [Parameter] readonly string KeyVaultClientSecret;
    [GitVersion(Framework = "netcoreapp3.1")] readonly GitVersion GitVersion;
    [GitRepository] readonly GitRepository GitRepository;

    [Parameter] readonly string NodePublishVersionOverride;
    [Parameter] readonly string PythonClientRepositoryTag;
    [Parameter] readonly string PhpClientRepositoryTag;

    [Parameter] readonly string CustomSwaggerDefinitionUrl;

    [Parameter] readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    [KeyVaultSecret] readonly string GitHubAuthenticationToken;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath OutputDirectory => RootDirectory / "output";

    Target Clean => _ => _
        .Executes(() =>
        {
            GlobDirectories(SourceDirectory, "**/bin", "**/obj").ForEach(DeleteDirectory);
            GlobDirectories(RootDirectory / "test", "**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore();
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(x => x
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetInformationalVersion(GitVersion.InformationalVersion));
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var testProjects = GlobFiles(RootDirectory / "test", "**/*.csproj");
            var testRun = 1;
            foreach (var testProject in testProjects)
            {
                var projectDirectory = Path.GetDirectoryName(testProject);
                string testFile = OutputDirectory / $"test_{testRun++}.testresults.xml";
                var dotnetPath = ToolPathResolver.GetPathExecutable("dotnet");

                StartProcess(dotnetPath, "test " +
                                         "--no-build " +
                                         "--test-adapter-path:. " +
                                         $"\"--logger:xunit;LogFilePath={testFile}\"",
                        workingDirectory: projectDirectory)
                    // AssertWairForExit() instead of AssertZeroExitCode()
                    // because we want to continue all tests even if some fail
                    .AssertWaitForExit();
            }
        });

    Target Publish => _ => _
        .DependsOn(GenerateClients)
        .Executes(async () =>
        {
            var publishDir = OutputDirectory / "publish";
            var zipPath = OutputDirectory / "AVACloud.Client.Generator.zip";

            DotNetPublish(x => x
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetProject(SourceDirectory / "Dangl.AVACloudClientGenerator" / "Dangl.AVACloudClientGenerator.csproj")
                .SetOutput(publishDir));

            System.IO.Compression.ZipFile.CreateFromDirectory(publishDir, zipPath);

            var repositoryInfo = GetGitHubRepositoryInfo(GitRepository);

            var isPrerelease = !(GitVersion.BranchName.Equals("master") || GitVersion.BranchName.Equals("origin/master"));

            var artifactPaths = new string[] { zipPath }.Concat(GlobFiles(OutputDirectory, "*.zip")).Distinct().ToArray();

            await PublishRelease(x => x
                .SetArtifactPaths(artifactPaths)
                .SetCommitSha(GitVersion.Sha)
                .SetRepositoryName(repositoryInfo.repositoryName)
                .SetRepositoryOwner(repositoryInfo.gitHubOwner)
                .SetTag(GitVersion.NuGetVersion)
                .SetPrerelease(isPrerelease)
                .SetToken(GitHubAuthenticationToken));
        });

    Target GenerateClients => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var languages = new[] { "Java", "TypeScriptNode", "JavaScript", "Php", "Python" };

            foreach (var language in languages)
            {
                GenerateClient(language);
            }
        });

    private void GenerateClient(string language)
    {
        var generatorPath = SourceDirectory / "Dangl.AVACloudClientGenerator" / "bin" / Configuration / "net7.0" / "Dangl.AVACloudClientGenerator.dll";
        var outputPath = OutputDirectory / language;


        var arguments = $"\"{generatorPath}\" -l {language} -o \"{outputPath}\"";
        if (!string.IsNullOrWhiteSpace(CustomSwaggerDefinitionUrl))
        {
            Serilog.Log.Information("Using custom Swagger definition url: " + CustomSwaggerDefinitionUrl);
            arguments += $" -u {CustomSwaggerDefinitionUrl}";
        }

        StartProcess(ToolPathResolver.GetPathExecutable("dotnet"), arguments)
            .AssertZeroExitCode();

        System.IO.Compression.ZipFile.CreateFromDirectory(outputPath, outputPath.ToString().TrimEnd('/').TrimEnd('\\') + ".zip");
    }

    Target GenerateAndPublishTypeScriptNpmClient => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            GenerateClient("TypeScriptNode");

            var clientRoot = OutputDirectory / "TypeScriptNode";
            var clientDir = clientRoot / "typescript-node-client";

            CopyFile(clientRoot / "README.md", clientDir / "README.md");
            CopyFile(clientRoot / "LICENSE.md", clientDir / "LICENSE.md");

            if (!string.IsNullOrWhiteSpace(NodePublishVersionOverride))
            {
                Npm($"version {NodePublishVersionOverride}", clientDir);
            }

            NpmInstall(x => x.SetProcessWorkingDirectory(clientDir));
            NpmRun(x => x.SetProcessWorkingDirectory(clientDir).SetProcessArgumentConfigurator(a => a.Add("build")));

            Npm("publish --access=public", clientDir);
        });

    Target GenerateAndPublishJavaScriptNpmClient => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            GenerateClient("JavaScript");

            var clientRoot = OutputDirectory / "JavaScript";
            var clientDir = clientRoot / "javascript-client";

            MoveFile(clientDir / "README.md", clientDir / "API_README.md");
            CopyFile(clientRoot / "README.md", clientDir / "README.md");
            CopyFile(clientRoot / "LICENSE.md", clientDir / "LICENSE.md");

            if (!string.IsNullOrWhiteSpace(NodePublishVersionOverride))
            {
                Npm($"version {NodePublishVersionOverride}", clientDir);
            }

            NpmInstall(x => x.SetProcessWorkingDirectory(clientDir));
            NpmRun(x => x.SetProcessWorkingDirectory(clientDir).SetProcessArgumentConfigurator(a => a.Add("build")));

            Npm("publish --access=public", clientDir);
        });

    Target GenerateAndPublishPythonClient => _ => _
        .DependsOn(Compile)
        .Requires(() => PythonClientRepositoryTag)
        .Executes(async () =>
        {
            await GenerateAndPushPythonCode("master", PythonClientRepositoryTag, false);
            await GenerateAndPushPythonCode("python3", $"{PythonClientRepositoryTag}-V3", true);
        });

    private async Task GenerateAndPushPythonCode(string branchName,
        string tag,
        bool transformToV3)
    {
        var clientRoot = OutputDirectory / "Python";
        var clientDir = clientRoot / "python-client";
        
        EnsureCleanDirectory(clientRoot);
        EnsureCleanDirectory(clientDir);

        GenerateClient("Python");

        MoveFile(clientDir / "README.md", clientDir / "API_README.md");
        CopyFile(clientRoot / "README.md", clientDir / "README.md");
        CopyFile(clientRoot / "LICENSE.md", clientDir / "LICENSE.md");

        var mirrorRepoDir = OutputDirectory / "MirrorRepo";
        Directory.CreateDirectory(mirrorRepoDir);
        var mirrorRepoUrl = "https://github.com/Dangl-IT/avacloud-client-python.git";

        try
        {
            Git($"clone {mirrorRepoUrl} -b {branchName}", mirrorRepoDir)?.ToList().ForEach(x => Serilog.Log.Information(x.Text));
        }
        catch
        {
            // If the branch doesn't exist, it should be created
            Git($"clone {mirrorRepoUrl}", mirrorRepoDir)?.ToList().ForEach(x => Serilog.Log.Information(x.Text));
        }

        mirrorRepoDir = mirrorRepoDir / "avacloud-client-python";

        // Delete all but .git/ in cloned repo
        var dirs = Directory.EnumerateDirectories(mirrorRepoDir)
            .Where(d => !d.EndsWith(".git", StringComparison.OrdinalIgnoreCase));
        dirs.ForEach(DeleteDirectory);
        var files = Directory.EnumerateFiles(mirrorRepoDir)
            .ToList();
        files.ForEach(File.Delete);
        // Copy data into cloned repo
        var dirsToCopy = Directory.EnumerateDirectories(clientDir)
            .ToList();
        dirsToCopy.ForEach(d =>
        {
            var folderName = Path.GetFileName(d);
            CopyDirectoryRecursively(d, mirrorRepoDir / folderName);
        });
        var filesToCopy = Directory.EnumerateFiles(clientDir)
            .ToList();
        filesToCopy.ForEach(f =>
        {
            var fileName = Path.GetFileName(f);
            File.Copy(f, mirrorRepoDir / fileName);
        });

        if (transformToV3)
        {
            // Now, we also want to ensure that we have a Python3 client available
            var pythonSourceFiles = GlobFiles(mirrorRepoDir, "**/*.py");
            using var httpClient = new HttpClient();
            Serilog.Log.Information($"Transforming {pythonSourceFiles.Count} source files to Python3");
            var conversionTasks = new List<Task>();
            foreach (var pythonSourceFile in pythonSourceFiles)
            {
                conversionTasks.Add(ConvertFileFromPython2ToPython3Async(pythonSourceFile, httpClient));
            }

            await Task.WhenAll(conversionTasks);
        }

        using (SwitchWorkingDirectory(mirrorRepoDir))
        {
            Git("add -A");
            var commitMessage = "Auto generated commit";
            Git($"commit -m \"{commitMessage}\"");
            Git($"tag \"{tag}\"");
            Git($"push --set-upstream origin {branchName}");
            Git("push --tags");
        }
    }

    private async Task ConvertFileFromPython2ToPython3Async(string filePath, HttpClient httpClient)
    {
        var python2Code = ReadAllText(filePath);

        var formValue = new Dictionary<string, string>
        {
            { "input", python2Code }
        };
        var formContent = new FormUrlEncodedContent(formValue);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://python2to3.com/");
        request.Content = formContent;

        var response = await httpClient.SendAsync(request);
        Assert.True(response.IsSuccessStatusCode);
        var responseHtml = await response.Content.ReadAsStringAsync();
        var htmlDoc = new HtmlAgilityPack.HtmlDocument();
        htmlDoc.LoadHtml(responseHtml);
        var python3Code = htmlDoc.DocumentNode.SelectSingleNode("//textarea[@id='result']").InnerText;
        python3Code = HttpUtility.HtmlDecode(python3Code);

        WriteAllText(filePath, python3Code);
    }

    Target GenerateAndPublishPhpClient => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            GenerateClient("Php");

            var composerJsonFile = GlobFiles(OutputDirectory, "**/*composer.json").Single();
            var composerJson = ReadAllText(composerJsonFile);
            var composerJObject = JObject.Parse(composerJson);
            var composerVersion = string.IsNullOrWhiteSpace(PhpClientRepositoryTag)
                ? GitVersion.NuGetVersion
                : PhpClientRepositoryTag;
            composerJObject["version"] = composerVersion;
            WriteAllText(composerJsonFile, composerJObject.ToString());


            var clientRoot = OutputDirectory / "Php";
            var clientDir = clientRoot / "php-client" / "Dangl" / "AVACloud";

            CopyFile(clientRoot / "README.md", clientDir / "CLIENT_README.md");
            CopyFile(clientRoot / "LICENSE.md", clientDir / "LICENSE.md");

            var mirrorRepoDir = OutputDirectory / "MirrorRepo";
            Directory.CreateDirectory(mirrorRepoDir);
            var mirrorBranchName = "master";
            var mirrorRepoUrl = "https://github.com/Dangl-IT/avacloud-client-php.git";

            try
            {
                Git($"clone {mirrorRepoUrl} {mirrorRepoDir} -b {mirrorBranchName}", mirrorRepoDir)?.ToList().ForEach(x => Serilog.Log.Information(x.Text));
            }
            catch
            {
                // If the branch doesn't exist, it should be created
                Git($"clone {mirrorRepoUrl}", mirrorRepoDir)?.ToList().ForEach(x => Serilog.Log.Information(x.Text));
            }

            // Delete all but .git/ in cloned repo
            var dirs = Directory.EnumerateDirectories(mirrorRepoDir)
                .Where(d => !d.EndsWith(".git", StringComparison.OrdinalIgnoreCase));
            dirs.ForEach(DeleteDirectory);
            var files = Directory.EnumerateFiles(mirrorRepoDir)
                .ToList();
            files.ForEach(File.Delete);
            // Copy data into cloned repo
            var dirsToCopy = Directory.EnumerateDirectories(clientDir)
                .ToList();
            dirsToCopy.ForEach(d =>
            {
                var folderName = Path.GetFileName(d);
                CopyDirectoryRecursively(d, mirrorRepoDir / folderName);
            });
            var filesToCopy = Directory.EnumerateFiles(clientDir)
                .ToList();
            filesToCopy.ForEach(f =>
            {
                var fileName = Path.GetFileName(f);
                File.Copy(f, mirrorRepoDir / fileName);
            });

            var phpClientTag = PhpClientRepositoryTag;
            if (string.IsNullOrWhiteSpace(phpClientTag))
            {
                phpClientTag =$"v{GitVersion.NuGetVersion}";
            }

            using (SwitchWorkingDirectory(mirrorRepoDir))
            {
                Git("add -A");
                var commitMessage = "Auto generated commit";
                Git($"commit -m \"{commitMessage}\"");
                Git($"tag \"{phpClientTag}\"");
                Git($"push --set-upstream origin {mirrorBranchName}");
                Git("push --tags");
            }
        });
}
