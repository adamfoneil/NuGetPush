using CommandLine;
using NuGetPush.CLI;
using Serilog;

try
{
	var options = GetOptions(Environment.CurrentDirectory);
	if (options.ApiKey is null) throw new Exception("Missing API key.");

	Log.Logger = InitSerilog(options.LogPath);

	var packageFiles = GetPackageFiles(Environment.CurrentDirectory);
	if (!packageFiles.Any()) throw new Exception("No packages found.");

	foreach (var package in packageFiles)
	{
		
	}
}
catch (Exception exc)
{
	Log.Logger.Error(exc, "Error pushing packages.");
}
