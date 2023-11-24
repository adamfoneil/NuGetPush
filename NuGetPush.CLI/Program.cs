using CommandLine;
using NuGet.Versioning;
using NuGetPush.CLI;
using Serilog.Events;
using Serilog;

await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(async o =>
{
	Log.Logger = new LoggerConfiguration()
		.WriteTo.File(Path.Combine(o.LogPath, "NuGetPush.log"), LogEventLevel.Information, rollingInterval: RollingInterval.Month, retainedFileCountLimit: 3)
		.CreateLogger();

	var local = GetLocalPackageInfo(o.ProjectPath);
	var onlineVersion = await GetOnlinePackageVersion(local.PackageId, o.FeedUrl);

	if (local.Version > onlineVersion)
	{

	}

	await Log.CloseAndFlushAsync();
});



static Task<NuGetVersion> GetOnlinePackageVersion(string packageId, string feedUrl)
{
	throw new NotImplementedException();
}

static (NuGetVersion Version, string PackageId, string LocalFile) GetLocalPackageInfo(string path)
{	
	throw new NotImplementedException();
}