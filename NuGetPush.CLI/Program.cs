using CommandLine;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGetPush.CLI;
using Serilog;
using Serilog.Events;
using System.Text.Json;

MSBuildLocator.RegisterDefaults();

await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(async options =>
{	
	try
	{
		var defaults = GetDefaultOptions(options.ProjectPath);

		options.LogPath ??= defaults.LogPath;
		options.FeedUrl ??= defaults.FeedUrl ?? throw new Exception("Missing the feed url");
		options.ApiKey ??= defaults.ApiKey ?? throw new Exception("Missing an API key");
		options.PushFromBranch ??= defaults.PushFromBranch;

		Log.Logger = new LoggerConfiguration()
			.WriteTo.File(Path.Combine(options.LogPath, "NuGetPush.log"), LogEventLevel.Information, rollingInterval: RollingInterval.Month, retainedFileCountLimit: 3)
			.CreateLogger();

		var local = GetLocalPackageInfo(options.ProjectPath);
		var onlineVersion = await GetOnlinePackageVersion(local.PackageId, options.FeedUrl, options.ApiKey);

		if (local.Version > onlineVersion)
		{
			Log.Logger.Information(
				"Local version of {packageId} is {localVersion} which is newer than the online version {onlineVersion}", 
				local.PackageId, local.Version, onlineVersion);

			if (!string.IsNullOrWhiteSpace(options.PushFromBranch))
			{
				var gitRepo = FindGetRepository(options.ProjectPath);
				if (gitRepo.Success)
				{					
					if (!gitRepo.CurrentBranch.Equals(options.PushFromBranch, StringComparison.CurrentCultureIgnoreCase))
					{
						Log.Logger.Information(
							"Current branch {currentBranch} is different from required push branch {pushBranch}, so package won't be published",
							gitRepo.CurrentBranch, options.PushFromBranch);
						return;
					}
				}				
			}

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"Publishing new version {local.Version}");
			Log.Logger.Information("Publishing new version {version}", local.Version);

			await PushPackageAsync(local.PackageId, options.FeedUrl, options.ApiKey, local.LocalFile);
		}
	}
	catch (Exception exc)
	{
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine(exc.Message);
		Log.Logger.Error(exc, "Error in main.");
	}
	
	await Log.CloseAndFlushAsync();
});

static async Task PushPackageAsync(string packageId, string feedUrl, string apiKey, string localFile)
{
	try
	{
		var repository = Repository.Factory.GetCoreV3(feedUrl);
		var resource = await repository.GetResourceAsync<PackageUpdateResource>();
		var cache = new SourceCacheContext();

		await resource.Push([localFile], symbolSource: null,
			timeoutInSecond: 120,
			disableBuffering: false,
			getApiKey: packageSource => apiKey,
			getSymbolApiKey: packageSource => null,
			noServiceEndpoint: false,
			skipDuplicate: false,
			symbolPackageUpdateResource: null,
			NullLogger.Instance);
	}
	catch (Exception exc)
	{
		Log.Logger.Error(exc, "Error pushing package");
		throw;
	}
}

// searches upwards from a given path to try to find the enclosing git repository
static (bool Success, string Result, string CurrentBranch) FindGetRepository(string projectPath)
{
	string path = projectPath;

	do
	{
		try
		{
			using var repo = new LibGit2Sharp.Repository(path);
			return (true, path, repo.Head.FriendlyName);
		}
		catch 
		{
			path = Directory.GetParent(path)?.FullName!;
			if (path is null) break;
		}
	} while (true);

	return (false, string.Empty, string.Empty);
}

// searches upward from a given path for a global config file of settings
static Options GetDefaultOptions(string path)
{
	const string BaseFilename = "nugetpush.json";

	string configFile;

	do
	{
		var parent = Directory.GetParent(path) ?? throw new Exception("Already at the root");		
		configFile = Path.Combine(parent.FullName, BaseFilename);
		if (File.Exists(configFile)) break;	
		path = parent.FullName;
	} while (!File.Exists(configFile));

	var json = File.ReadAllText(configFile);
	return JsonSerializer.Deserialize<Options>(json) ?? new();
}

static async Task<NuGetVersion> GetOnlinePackageVersion(string packageId, string feedUrl, string apiKey)
{
	const string DefaultMinVersion = "0.0.0";

	var repository = Repository.Factory.GetCoreV3(feedUrl);
	var resource = await repository.GetResourceAsync<PackageMetadataResource>();
	var cache = new SourceCacheContext();

	Console.WriteLine($"Searching for {packageId} metadata...");
	var results = await resource.GetMetadataAsync(packageId, includePrerelease: true, includeUnlisted: true, cache, NullLogger.Instance, CancellationToken.None);

	if (results.Any())
	{
		var latest = results.Last();
		return latest.Identity.Version;
	}

	return NuGetVersion.Parse(DefaultMinVersion);
}

static (NuGetVersion Version, string PackageId, string LocalFile) GetLocalPackageInfo(string path)
{	
	var projectFile = FindProjectFile(path);
	var collection = new ProjectCollection();
	var project = collection.LoadProject(projectFile);

	var version = NuGetVersion.Parse(project.GetPropertyValue("Version"));
	var packageId = project.GetPropertyValue("PackageId");
	var packagePath = Path.GetFullPath(Path.Combine(path, project.GetPropertyValue("PackageOutputPath")));

	var packageFile = packageId + "." + version + ".nupkg";

	var packageFullPath = Path.Combine(packagePath, packageFile);
	if (!File.Exists(packageFullPath)) throw new FileNotFoundException($"Package file does not exist: {packageFullPath}");

	return (
		version,
		packageId,
		packageFullPath
		);
}

static string FindProjectFile(string path) =>
	Directory.GetFiles(path, "*.csproj").FirstOrDefault() ?? throw new FileNotFoundException($"Couldn't find .csproj file in {path}");
