using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
/*
mac
linux
*/

    public static int Main () {
        if(IsLocalBuild) {
            config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddUserSecrets<Build>()
                .Build();
        }
        
        return Execute<Build>(x => x.Compile);
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;

    public static IConfiguration config;
    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath TempDirectory => RootDirectory / "temp";
    AbsolutePath CommandLineDirectory => TempDirectory / "exe";
    AbsolutePath MsBuildDirectory => TempDirectory / "msbuild";
    AbsolutePath PowerShellDirectory => TempDirectory / "powershell";
    AbsolutePath DeployDirectory => RootDirectory / "deploy";
    AbsolutePath PackagingDirectory => RootDirectory / "packaging";

    String AssemblyProduct = "Pickles";
    String AssemblyCompany = "Pickles";
    String Version = "3.0.0-alpha";
    String Copyright = "Copyright (c) Jeffrey Cameron 2010-2012, PicklesDoc 2012-present";
    String NuGetApiKey = "";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(TempDirectory);
            EnsureCleanDirectory(DeployDirectory);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Test => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(RootDirectory / "src/Pickles/Pickles.sln")
                .EnableNoRestore()
            );
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "src/Pickles/Pickles.CommandLine/Pickles.CommandLine.csproj")
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetProperty("Version", Version)
                .SetOutputDirectory(CommandLineDirectory)
            //.SetCopyright(Copyright)
            );

            DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "src/Pickles/Pickles.MsBuild/Pickles.MsBuild.csproj")
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetProperty("Version", Version)
                .SetOutputDirectory(MsBuildDirectory)
                //.SetCopyright(Copyright)
            );

            DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "src/Pickles/Pickles.PowerShell/Pickles.PowerShell.csproj")
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetProperty("Version", Version)
                .SetOutputDirectory(PowerShellDirectory)
                //.SetCopyright(Copyright)
            );

            Directory.CreateDirectory(DeployDirectory);

            ZipFile.CreateFromDirectory(CommandLineDirectory, DeployDirectory / "Pickles-exe-" + Version + ".zip");
            ZipFile.CreateFromDirectory(MsBuildDirectory, DeployDirectory / "Pickles-msbuild-" + Version + ".zip");
            ZipFile.CreateFromDirectory(PowerShellDirectory, DeployDirectory / "Pickles-powershell-" + Version + ".zip");
        });

    Target Chocolatey => _ => _
        .DependsOn(Compile)
        .Executes(() => {
            Directory.CreateDirectory(PackagingDirectory);
            
            File.Copy(RootDirectory / "LICENSE.txt", CommandLineDirectory / "LICENSE.txt", true);
            File.Copy(RootDirectory / "VERIFICATION.txt", CommandLineDirectory / "VERIFICATION.txt", true);

            DotNetPack(s => s
                .SetProject(RootDirectory / "src/Pickles/Pickles.CommandLine/Pickles.CommandLine.csproj")
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .SetProperty("Version", Version)
                .SetOutputDirectory(DeployDirectory)
                .SetProperty("NuspecFile", RootDirectory / "chocolatey/pickles.nuspec")
            );
        });

    Target PublishNuGet => _ => _
        .DependsOn(Compile)
        .Executes(() => {
            if(IsLocalBuild) {
                NuGetApiKey = config["NugetApiKey"];
                Console.WriteLine(NuGetApiKey);
            }

            NuGetTasks.NuGetPush(s => s
                .SetSource("https://www.nuget.org/api/v2/package")
                .SetApiKey(NuGetApiKey)
                .SetTargetPath(PowerShellDirectory / $"Pickles.{Version}.nupkg")
            );

            NuGetTasks.NuGetPush(s => s
                .SetSource("https://www.nuget.org/api/v2/package")
                .SetApiKey(NuGetApiKey)
                .SetTargetPath(CommandLineDirectory / $"Pickles.CommandLine.{Version}.nupkg")
            );

            NuGetTasks.NuGetPush(s => s
                .SetSource("https://www.nuget.org/api/v2/package")
                .SetApiKey(NuGetApiKey)
                .SetTargetPath(MsBuildDirectory / $"Pickles.MsBuild.{Version}.nupkg")
            );
        });


}
