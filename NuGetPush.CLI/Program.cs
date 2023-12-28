using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Serilog;

try
{
	var options = GetOptions(Environment.CurrentDirectory);
	if (options.ApiKey is null) throw new ArgumentException("Missing API key.");

	Log.Logger = InitSerilog(options.LogPath);

	var packageFiles = GetPackageFiles(Environment.CurrentDirectory);
	if (!packageFiles.Any()) throw new ArgumentException("No packages found.");

	var repository = Repository.Factory.GetCoreV3(options.FeedUrl);
	var resource = await repository.GetResourceAsync<PackageUpdateResource>();

	// the package and symbols are pushed together
	foreach (var packageGrp in packageFiles.GroupBy(file => file.PackageId))
	{
		await resource.Push(
			packageGrp.Select(p => p.Path).ToArray(), 
			symbolSource: options.SymbolFeedUrl,
			timeoutInSecond: 120,
			disableBuffering: false,
			getApiKey: packageSource => options.ApiKey,
			getSymbolApiKey: packageSource => options.ApiKey,
			noServiceEndpoint: false,
			skipDuplicate: true,
			symbolPackageUpdateResource: null,
			NullLogger.Instance);

		Log.Logger.Information("Pushed {packageId}", packageGrp.Key);

		// no need to keep these files locally
		foreach (var file in packageGrp) File.Delete(file.Path);
	}
}
catch (ArgumentException exc)
{
	// don't need to log "argument" exceptions
	Console.ForegroundColor = ConsoleColor.Red;
	Console.WriteLine(exc.Message);
}
catch (Exception exc)
{
	Log.Logger.Error(exc, "Error pushing packages.");
}
