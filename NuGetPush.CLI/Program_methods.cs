﻿using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGetPush.CLI;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

internal enum FileType
{
	Package,
	Symbols
}

internal partial class Program
{
	static Serilog.ILogger InitSerilog(string path) => new LoggerConfiguration()
		.WriteTo.File(Path.Combine(path, "NuGetPush.log"), LogEventLevel.Information, rollingInterval: RollingInterval.Month, retainedFileCountLimit: 3)
		.WriteTo.Console(LogEventLevel.Information)
		.CreateLogger();

	static IEnumerable<(FileType FileType, string PackageId, string Path)> GetPackageFiles(string path)
	{
		var allFiles = Directory.GetFiles(path, "*.nupkg", SearchOption.TopDirectoryOnly);
		HashSet<string> results = [];

		foreach (var file in allFiles)
		{
			if (IsFileType(file, ".symbols.nupkg", out var symbolPackage) && results.Add(file))
			{
				yield return (FileType.Symbols, ParsePackageId(symbolPackage), file);
			}

			if (IsFileType(file, ".nupkg", out var package) && results.Add(file))
			{
				yield return (FileType.Package, ParsePackageId(package), file);
			}
		}
	}

	static bool IsFileType(string path, string suffix, out string parsedPath)
	{
		var result = path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
		var lastDir = path.LastIndexOf('\\') + 1;
		parsedPath = (result) ? path[lastDir..^suffix.Length] : path;
		return result;
	}

	static string ParsePackageId(string path) => Regex.Replace(path, @".(\d+).(\d+).(\d+)", (match) => string.Empty);


	static async Task PushPackageAsync(PackageUpdateResource resource, string packageId, string feedUrl, string apiKey, string localFile)
	{
		try
		{						
			var cache = new SourceCacheContext();

			await resource.Push([localFile], symbolSource: null,
				timeoutInSecond: 120,
				disableBuffering: false,
				getApiKey: packageSource => apiKey,
				getSymbolApiKey: packageSource => null,
				noServiceEndpoint: false,
				skipDuplicate: true,
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
	static (bool Success, string Result, string CurrentBranch) FindGitRepository(string projectPath)
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

	// searches upward from a given path for a global config file of settings.
	// this is how you can make your API key available to all projects in your base dev directory,
	// keeping it out of source control, but making this convenient to use with any project
	static Options GetOptions(string path)
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

	static async Task<NuGetVersion> GetOnlinePackageVersionAsync(string packageId, string feedUrl)
	{
		const string DefaultMinVersion = "0.0.0";

		var repository = Repository.Factory.GetCoreV3(feedUrl);
		var resource = await repository.GetResourceAsync<PackageMetadataResource>();
		var cache = new SourceCacheContext();

		Log.Logger.Information("Searching for {packageId} metadata...", packageId);		
		var results = await resource.GetMetadataAsync(packageId, includePrerelease: true, includeUnlisted: true, cache, NullLogger.Instance, CancellationToken.None);

		if (results.Any())
		{
			var latest = results.Last();
			return latest.Identity.Version;
		}

		return NuGetVersion.Parse(DefaultMinVersion);
	}

	static (NuGetVersion Version, string PackageId, string LocalFile) GetLocalPackageInfo(bool usingPostBuildEvent, string path, Action<string> buildAction)
	{
		var projectFullPath = FindProjectFile(path);
		var collection = new ProjectCollection();
		var project = collection.LoadProject(projectFullPath);

		var generateOnBuild = bool.Parse(project.GetProperty("GeneratePackageOnBuild")?.EvaluatedValue ?? "false");
		
		if (usingPostBuildEvent && generateOnBuild)
		{			
			throw new ArgumentException("Can't use the 'GeneratePackageOnBuild' option because generated packages aren't available to programs during the Post Build event.");
		}

		var version = NuGetVersion.Parse(project.GetPropertyValue("Version"));
		var packageId = project.GetPropertyValue("PackageId");
		var packagePath = Path.GetFullPath(Path.Combine(path, project.GetPropertyValue("PackageOutputPath")));

		var packageFile = packageId + "." + version + ".nupkg";
		var packageFullPath = Path.Combine(packagePath, packageFile);

		if (usingPostBuildEvent)
		{
			// package and symbols need to be built. This is a separate method to keep certain
			// basics stable above with the build internals more configurable externally
			buildAction.Invoke(projectFullPath);
		}

		return (version, packageId, packageFullPath);
	}

	static void BuildPackage(string projectFile)
	{
		BuildManager.DefaultBuildManager.BeginBuild(new BuildParameters(new ProjectCollection()));

		try
		{
			Dictionary<string, string> properties = new()
			{
				["Configuration"] = "Release",
				// it's important that you don't run any post-build event otherwise you get recursion
				// because this console app was triggered by a post-build event
				["PostBuildEvent"] = string.Empty
			};

			var request = new BuildRequestData(projectFile, properties, null, ["Pack"], null);
			var result = BuildManager.DefaultBuildManager.BuildRequest(request);
			if (result.OverallResult != BuildResultCode.Success) throw result.Exception;
		}
		finally
		{
			BuildManager.DefaultBuildManager.EndBuild();
		}		
	}

	static (bool Success, string Message) RunProcess(string exe, string arguments, string workingDirectory, int successResult = 0)
	{
		ProcessStartInfo psi = new(exe, arguments)
		{
			WorkingDirectory = workingDirectory,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};

		var process = Process.Start(psi) ?? throw new Exception($"Couldn't start process {exe}");
		var errors = process.StandardError.ReadToEnd();

		process.WaitForExit();

		if (process.ExitCode != successResult)
		{
			return (false, errors);
		}

		return (true, default!);
	}

	static string FindProjectFile(string path) =>
		Directory.GetFiles(path, "*.csproj").FirstOrDefault() ?? throw new FileNotFoundException($"Couldn't find .csproj file in {path}");

	static (bool Result, string? CsProjectFilename, string? CsProjectDir) IsProjectPath(string path)
	{
		try
		{
			var projectFile = FindProjectFile(path);
			return (true, projectFile, Path.GetDirectoryName(projectFile));
		}
		catch 
		{
			return (false, default, default);
		}
	}
}
