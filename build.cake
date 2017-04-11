#tool "GitVersion.CommandLine"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target                  = Argument("target", "Default");
var configuration           = Argument("configuration", "Release");
var solutionPath            = MakeAbsolute(File(Argument("solutionPath", "./Nest.Indexify.sln")));
var framework               = Argument("framework", "netcoreapp1.1");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

var artifacts               = MakeAbsolute(Directory(Argument("artifactPath", "./artifacts")));
var versionAssemblyInfo     = MakeAbsolute(File(Argument("versionAssemblyInfo", "VersionAssemblyInfo.cs")));

IEnumerable<FilePath> nugetProjectPaths     = null;
SolutionParserResult solution               = null;
GitVersion versionInfo                      = null;

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Setup(ctx => {
    if(!FileExists(solutionPath)) throw new Exception(string.Format("Solution file not found - {0}", solutionPath.ToString()));
    solution = ParseSolution(solutionPath.ToString());

    Information("[Setup] Using Solution '{0}'", solutionPath.ToString());

    if(DirectoryExists(artifacts)) 
    {
        DeleteDirectory(artifacts, true);
    }
    
    EnsureDirectoryExists(artifacts);
    
    var binDirs = GetDirectories(solutionPath.GetDirectory() +@"\src\**\bin");
    var objDirs = GetDirectories(solutionPath.GetDirectory() +@"\src\**\obj");
    DeleteDirectories(binDirs, true);
    DeleteDirectories(objDirs, true);
});

Task("Update-Version-Info")
    .IsDependentOn("Create-Version-Info")
    .Does(() => 
{
        versionInfo = GitVersion(new GitVersionSettings {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFilePath = versionAssemblyInfo
        });

    if(versionInfo != null) {
        Information("Version: {0}", versionInfo.FullSemVer);
    } else {
        throw new Exception("Unable to determine version");
    }
});

Task("Create-Version-Info")
    .WithCriteria(() => !FileExists(versionAssemblyInfo))
    .Does(() =>
{
    Information("Creating version assembly info");
    CreateAssemblyInfo(versionAssemblyInfo, new AssemblyInfoSettings {
        Version = "0.0.0.0",
        FileVersion = "0.0.0.0",
        InformationalVersion = "",
    });
});

Task("DotNet-MsBuild-Clean")
    .Does(() => {

        MSBuild(solutionPath, c => c
            .SetConfiguration(configuration)
            .SetVerbosity(Verbosity.Minimal)
            .UseToolVersion(MSBuildToolVersion.VS2017)
            .WithTarget("Clean")
        );
});

Task("DotNet-MsBuild-Restore")
    .Does(() => {

        MSBuild(solutionPath, c => c
            .SetConfiguration(configuration)
            .SetVerbosity(Verbosity.Minimal)
            .UseToolVersion(MSBuildToolVersion.VS2017)
            .WithTarget("Clean;Restore")
        );
});

Task("DotNet-MsBuild")
    .IsDependentOn("Update-Version-Info")
    .IsDependentOn("Restore")
    .Does(() => {

        MSBuild(solutionPath, c => c
            .SetConfiguration(configuration)
            .SetVerbosity(Verbosity.Minimal)
            .UseToolVersion(MSBuildToolVersion.VS2017)
            .WithProperty("TreatWarningsAsErrors", "true")
            .WithTarget("Build")
        );

});

Task("DotNet-MsBuild-Pack")
    .IsDependentOn("Build")
    .Does(() => {

   var projectFiles = GetFiles("src/**/*.csproj");

    foreach(var projectFile in projectFiles) {
        MSBuild(projectFile, c => c
            .SetConfiguration(configuration)
            .SetVerbosity(Verbosity.Normal)
            .UseToolVersion(MSBuildToolVersion.VS2017)
            .WithProperty("PackageVersion", versionInfo.NuGetVersionV2)
            .WithProperty("NoBuild", "true")
            .WithTarget("Pack"));
    }
});

Task("DotNet-MsBuild-Publish")
    .IsDependentOn("Build")
    .Does(() => {

    var buildOutput = artifacts +"/build";
    EnsureDirectoryExists(buildOutput);
    var settings = new DotNetCorePublishSettings
    {
        Framework = "netcoreapp1.1",
        Configuration = configuration,
        OutputDirectory = buildOutput
    };

    DotNetCorePublish(solutionPath.ToString(), settings);
    Zip(buildOutput, artifacts + "/Publish.zip");

});

Task("DotNet-MsBuild-CopyToArtifacts")
    .IsDependentOn("DotNet-MsBuild-Pack")
    .Does(() => {

        EnsureDirectoryExists(artifacts);
        CopyFiles("src/**/bin/" +configuration +"/*.nupkg", artifacts);
});

Task("DotNet-Test")
    .IsDependentOn("Build")
    .Does(() => {

    var settings = new DotNetCoreTestSettings
    {
        Configuration = configuration,
        NoBuild = true
    };

    var projectFiles = GetFiles("tests/**/*.csproj");

    foreach(var projectFile in projectFiles) {
        DotNetCoreTest(projectFile.ToString(), settings);
    }
});

Task("AppVeyor-Update-Build-Number")
    .IsDependentOn("Update-Version-Info")
    .WithCriteria(() => AppVeyor.IsRunningOnAppVeyor)
    .Does(() =>
{
    AppVeyor.UpdateBuildVersion(versionInfo.FullSemVer +"|" +AppVeyor.Environment.Build.Number);
});

Task("Appveyor-Upload-Artifacts")
    .IsDependentOn("Package")
    .WithCriteria(() => AppVeyor.IsRunningOnAppVeyor)
    .Does(() =>
{
    foreach(var nupkg in GetFiles(artifacts +"/*.nupkg")) {
        AppVeyor.UploadArtifact(nupkg);
    }
});

Task("Appveyor")
    .WithCriteria(() => AppVeyor.IsRunningOnAppVeyor)
    .IsDependentOn("AppVeyor-Update-Build-Number")
    .IsDependentOn("AppVeyor-Upload-Artifacts");

// ************************** //

Task("Clean")
    .IsDependentOn("DotNet-MsBuild-Clean");

Task("Restore")
    .IsDependentOn("DotNet-MsBuild-Restore");

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("DotNet-MsBuild");

Task("Test")
    .IsDependentOn("Build")
    .IsDependentOn("DotNet-Test");

Task("Package")
    .IsDependentOn("Build")
    .IsDependentOn("DotNet-MsBuild-CopyToArtifacts")
    .IsDependentOn("DotNet-MsBuild-Pack");

Task("CI")
    .IsDependentOn("AppVeyor")
    .IsDependentOn("Default");

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
