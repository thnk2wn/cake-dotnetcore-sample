#load utility.cake

#addin "Cake.FileHelpers"
#addin "Cake.Karma"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var _targetArg = Argument("target", "Default");
var _configArg = Argument("configuration", "Debug");
var _frameworkArg = Argument("framework", "netcoreapp2.0");

//////////////////////////////////////////////////////////////////////
// Script level variables and constants
//////////////////////////////////////////////////////////////////////

private const string _projectName = "FavGif";

var _slnDir = Directory("..");
var _slnFile = _slnDir + File($"{_projectName}.Web.sln");

var _webDir = _slnDir + Directory($"{_projectName}.Web");
var _cliDir = _slnDir + Directory($"{_projectName}.CLI");
var _apiDir = _slnDir + Directory($"{_projectName}.API");

private readonly string _binPath = $"bin/{_configArg}/{_frameworkArg}";
private readonly CakeUtility _utility = new CakeUtility(Context);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    _utility.CleanDir(_webDir + Directory(_binPath));
    _utility.CleanDir(_cliDir + Directory(_binPath));

    _utility.CleanDir(_apiDir + Directory("_package"));
    _utility.CleanDir(_webDir + Directory("_package"));

    _utility.CleanDir(Directory("./output/_package"));
});

Task("Restore-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore(_slnFile);
    _utility.NPMInstall(_apiDir);
    _utility.NPMInstall(_webDir);
});

Task("Build")
    .IsDependentOn("Restore-Packages")
    .Does(() =>
{
    var buildSettings = new DotNetCoreBuildSettings
    {
        Framework = _frameworkArg,
        Configuration = _configArg,
        Verbosity = DotNetCoreVerbosity.Normal
    };

    Information($"Building solution with config {_configArg}, framework {_frameworkArg}");
    DotNetCoreBuild(_slnFile, buildSettings);
    Information($"Built solution with config {_configArg}, framework {_frameworkArg}");
});

Task("Test-Web-JS-Unit")
    .IsDependentOn("Build")
    .Does(() =>
{    
    var settings = new KarmaStartSettings
    {
        ConfigFile = _webDir + File("ClientApp/test/karma.conf.js"),
        LogLevel = KarmaLogLevel.Info,

        // Exit browser when done, don't hang up build script
        SingleRun = true,

        // Will get a bunch of TypeScript errors if we don't set working directory to web dir
        WorkingDirectory = _webDir,

        // Don't rely on global install of karma
        LocalKarmaCli = _webDir + File("node_modules/karma-cli/bin/karma"),
        RunMode = KarmaRunMode.Local
    };

    Information($"Running web javascript tests using {settings.ConfigFile}, {settings.LocalKarmaCli}");

    // Cake.Karma doesn't currently support latest Cake version. build.sh and build.ps1 changed to use "--settings_skipverification=true"
    // https://github.com/cake-contrib/Cake.Karma/issues/10
    // We could also consider NPM test or Cake's StartProcess with "karma start"
    KarmaStart(settings);
});

Task("Test-Web-DotNet-Unit")
    .IsDependentOn("Build")
    .Does(() =>
{
    var testProject = $"{_projectName}.Web.UnitTests";
    var testAssembly = File(_slnDir + File($"{testProject}/{testProject}.csproj"));

    _utility.DotNetCoreXUnit(
        projectPattern: testAssembly.ToString(),
        config: _configArg);
});

Task("Test-Unit")
    .IsDependentOn("Test-Web-DotNet-Unit")
    .IsDependentOn("Test-Web-JS-Unit")
    .Does(() =>
{
});

Task("Package")
    .IsDependentOn("Test-Unit")
    .Does(() =>
{
    // Package node.js web api with Gulp
    _utility.GulpRun(path: _apiDir, taskArg: "package");

    // Package website. Make sure webpack-cli package is installed locally in website.
    var settings = new DotNetCorePublishSettings
    {
        Framework = _frameworkArg,
        Configuration = _configArg,
        OutputDirectory = _webDir + Directory("_package")
    };

    var project = _webDir + File($"{_projectName}.Web.csproj");
    Information($"Publishing asp.net core website {project}");
    DotNetCorePublish(project, settings);

    // Zip deployable files. Useful for artifacts, uploads to veracode security scans, etc.
    _utility.ZipDir(_apiDir + Directory("_package"), "./output/_package/api.zip");
    _utility.ZipDir(_webDir + Directory("_package"), "./output/_package/website.zip");

    // Not the entire directory just executable files in root, excludes some subfolders like localization etc.
    _utility.ZipBinFiles(_cliDir + Directory(_binPath), "./output/_package/cli.zip");
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Package");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(_targetArg);