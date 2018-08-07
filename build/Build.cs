using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using System.IO;
using static Nuke.Common.Tooling.ProcessTasks;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Azure.KeyVault;
using Nuke.GitHub;
using static Nuke.GitHub.GitHubTasks;
using Nuke.Common.Git;

[KeyVaultSettings(
    VaultBaseUrlParameterName = nameof(KeyVaultBaseUrl),
    ClientIdParameterName = nameof(KeyVaultClientId),
    ClientSecretParameterName = nameof(KeyVaultClientSecret))]
class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter] string KeyVaultBaseUrl;
    [Parameter] string KeyVaultClientId;
    [Parameter] string KeyVaultClientSecret;
    [GitVersion] readonly GitVersion GitVersion;
    [GitRepository] readonly GitRepository GitRepository;

    [KeyVaultSecret] string GitHubAuthenticationToken;

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

                StartProcess(dotnetPath, "xunit " +
                                         "-nobuild " +
                                         $"-xml {testFile.DoubleQuoteIfNeeded()}",
                        workingDirectory: projectDirectory)
                    // AssertWairForExit() instead of AssertZeroExitCode()
                    // because we want to continue all tests even if some fail
                    .AssertWaitForExit();
            }
        });

    Target Publish => _ => _
        .DependsOn(Compile)
        .Executes(async () =>
        {
            var publishDir = OutputDirectory / "publish";
            var zipPath = OutputDirectory / "AVACloud.Client.zip";

            DotNetPublish(x => DefaultDotNetPublish
                .SetProject(SourceDirectory / "Dangl.AVACloudClientGenerator" / "Dangl.AVACloudClientGenerator.csproj")
                .SetOutput(publishDir));

            System.IO.Compression.ZipFile.CreateFromDirectory(publishDir, zipPath);

            var repositoryInfo = GetGitHubRepositoryInfo(GitRepository);

            var isPrerelease = !(GitVersion.BranchName.Equals("master") || GitVersion.BranchName.Equals("origin/master"));

            await PublishRelease(new GitHubReleaseSettings()
                .SetArtifactPaths(new string[] { zipPath })
                .SetCommitSha(GitVersion.Sha)
                .SetRepositoryName(repositoryInfo.repositoryName)
                .SetRepositoryOwner(repositoryInfo.gitHubOwner)
                .SetTag(GitVersion.NuGetVersion)
                .SetPrerelease(isPrerelease)
                .SetToken(GitHubAuthenticationToken));
        });
}
