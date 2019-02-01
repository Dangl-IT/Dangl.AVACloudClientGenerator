using Nuke.Azure.KeyVault;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities;
using Nuke.GitHub;
using System.IO;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tooling.ProcessTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.GitHub.GitHubTasks;
using static Nuke.Common.Tools.Npm.NpmTasks;
using static Nuke.Common.Tools.Git.GitTasks;
using System.Linq;
using System;
using System.Collections.Generic;

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
    [GitVersion] readonly GitVersion GitVersion;
    [GitRepository] readonly GitRepository GitRepository;

    [Parameter] readonly string NodePublishVersionOverride;

    [KeyVaultSecret] readonly string GitHubAuthenticationToken;

    Target Clean => _ => _
        .Executes(() =>
        {
            DeleteDirectories(GlobDirectories(SourceDirectory, "**/bin", "**/obj"));
            DeleteDirectories(GlobDirectories(RootDirectory / "test", "**/bin", "**/obj"));
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => DefaultDotNetRestore);
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => DefaultDotNetBuild
                .SetFileVersion(GitVersion.GetNormalizedFileVersion())
                .SetAssemblyVersion(GitVersion.AssemblySemVer));
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var testProjects = GlobFiles(SolutionDirectory / "test", "*.csproj");
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

            DotNetPublish(x => DefaultDotNetPublish
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
        var generatorPath = SourceDirectory / "Dangl.AVACloudClientGenerator" / "bin" / Configuration / "netcoreapp2.1" / "Dangl.AVACloudClientGenerator.dll";
        var outputPath = OutputDirectory / language;
        var generatorSettings = new ToolSettings()
            .SetToolPath(ToolPathResolver.GetPathExecutable("dotnet"))
            .SetArgumentConfigurator(a => a
                .Add(generatorPath)
                .Add("-l {value}", language)
                .Add("-o {value}", outputPath));
        StartProcess(generatorSettings)
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

            NpmInstall(x => x.SetWorkingDirectory(clientDir));
            NpmRun(x => x.SetWorkingDirectory(clientDir).SetArgumentConfigurator(a => a.Add("build")));

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

            NpmInstall(x => x.SetWorkingDirectory(clientDir));
            NpmRun(x => x.SetWorkingDirectory(clientDir).SetArgumentConfigurator(a => a.Add("build")));

            Npm("publish --access=public", clientDir);
        });

    Target GenerateAndPublishPythonClient => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            GenerateClient("Python");

            var clientRoot = OutputDirectory / "Python";
            var clientDir = clientRoot / "python-client";

            MoveFile(clientDir / "README.md", clientDir / "API_README.md");
            CopyFile(clientRoot / "README.md", clientDir / "README.md");
            CopyFile(clientRoot / "LICENSE.md", clientDir / "LICENSE.md");

            var mirrorRepoDir = OutputDirectory / "MirrorRepo";
            Directory.CreateDirectory(mirrorRepoDir);
            var mirrorBranchName = "master";
            var mirrorRepoUrl = "https://github.com/Dangl-IT/avacloud-client-python.git";

            try
            {
                Git($"clone {mirrorRepoUrl} -b {mirrorBranchName}", mirrorRepoDir)?.ToList().ForEach(x => Logger.Log(x.Text));
            }
            catch
            {
                // If the branch doesn't exist, it should be created
                Git($"clone {mirrorRepoUrl}", mirrorRepoDir)?.ToList().ForEach(x => Logger.Log(x.Text));
            }

            mirrorRepoDir += "avacloud-client-python"; 

            // Delete all but .git/ in cloned repo
            var dirs = Directory.EnumerateDirectories(mirrorRepoDir)
                .Where(d => !d.EndsWith(".git", StringComparison.OrdinalIgnoreCase));
            DeleteDirectories(dirs);
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

            Git("add -A", mirrorRepoDir);
            var commitMessage = "Auto generated commit";
            Git($"commit -m \"{commitMessage}\"", mirrorRepoDir);
            Git($"push --set-upstream origin {mirrorBranchName}", mirrorRepoDir);
        });
}
