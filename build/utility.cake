#addin nuget:?package=SharpZipLib
#addin nuget:?package=Cake.Compression

#addin "Cake.Incubator"

#addin "Cake.Npm"
#addin "Cake.Gulp"

class CakeUtility
{
    public CakeUtility(ICakeContext context)
    {
        this.Context = context;
    }

    private ICakeContext Context { get; }

    public void CleanDir(DirectoryPath path)
    {
        Context.Information($"Cleaning directory {path}");
        Context.CleanDirectory(path);
    }

    // https://github.com/cake-build/cake/issues/1872
    // https://andrewlock.net/running-tests-with-dotnet-xunit-using-cake/
    public void DotNetCoreXUnit(
        string projectPattern, 
        string config, 
        bool diagnostics = true,
        bool stopOnFirstFailure = false,
        bool parallel = true)
    {
        var projects = Context.GetFiles(projectPattern);

        if (projects.Count == 0) 
        {
            throw new System.IO.FileNotFoundException($"Failed to match any files to {projectPattern}");
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendFormat("-configuration {0}", config);

        if (diagnostics)
        {
            sb.AppendFormat(" -diagnostics");
        }

        if (stopOnFirstFailure) 
        {
            sb.AppendFormat(" -stoponfail");
        }

        if (parallel) 
        {
            sb.AppendFormat(" -parallel collections");
        }

        var args = sb.ToString();

        foreach (var project in projects)
        {
            Context.Information($"Running dotnet xunit for '{project.FullPath}' with args '{args}'");
            Context.DotNetCoreTool(projectPath: project.FullPath, command: "xunit", arguments: args);
        }
    }

    public void GulpRun(DirectoryPath path, string taskArg)
    {
        Context.Information($"Executing gulp for {path}");

        Context.Gulp().Local.Execute(settings => settings
            .SetPathToGulpJs(path + Context.File("/node_modules/gulp/bin/gulp.js"))
            .WithGulpFile(path + Context.File("/gulpfile.js"))
            .WithArguments(taskArg)
        );
    }

    public void NPMInstall(DirectoryPath path)
    {
        var settings = new Cake.Npm.Install.NpmInstallSettings();
        settings.LogLevel = NpmLogLevel.Info;
        settings.WorkingDirectory = path;
        Context.Information($"Running npm install for {path}");
        Context.NpmInstall(settings);
    }

    public void ZipDir(DirectoryPath dirPath, FilePath zipFile)
    {
        Context.Information($"Compressing {dirPath} to {zipFile}");
        Context.ZipCompress(dirPath, zipFile);
    }

    // i.e. ZipFiles(binDir, zipFile, "*.exe", "*.dll", "*.pdb", "*.config");
    public void ZipFiles(DirectoryPath sourceDir, FilePath zipFile, params string[] filePatterns) 
    {
        var fullSourceDir = Context.MakeAbsolute(sourceDir);
        var fullPatterns = filePatterns
            .Select(pattern => $"{fullSourceDir.FullPath}/{pattern}")
            .ToArray();
        
        // Cake.Incubator variant of GetFiles allowing multiple patterns    
        var files = Context.GetFiles(fullPatterns);
        var patternText = string.Join(",", filePatterns);
        Context.Information($"Compressing {files.Count} file(s) from {sourceDir} with patterns {patternText} to {zipFile}");
        Context.ZipCompress(sourceDir, zipFile, files);
    }

    public void ZipBinFiles(DirectoryPath binDir, FilePath zipFile)
    {
        // Exclude XML as currently just intellisense support xml files.
        ZipFiles(binDir, zipFile, "*.exe", "*.dll", "*.pdb", "*.config");
    }
}