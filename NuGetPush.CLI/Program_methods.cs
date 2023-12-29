using NuGetPush.CLI;
using NuGetPush.CLI.Static;
using Serilog;
using Serilog.Events;
using System.Text.Json;
using System.Text.RegularExpressions;

internal enum FileType
{
	Package,
	Symbols
}

internal partial class Program
{
	static Serilog.Core.Logger InitSerilog(string path) => new LoggerConfiguration()
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
				yield return (FileType.Symbols, RegexHelper.ParsePackageId(symbolPackage), file);
			}

			if (IsFileType(file, ".nupkg", out var package) && results.Add(file))
			{
				yield return (FileType.Package, RegexHelper.ParsePackageId(package), file);
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
}
