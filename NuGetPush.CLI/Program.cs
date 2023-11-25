using CommandLine;
using Microsoft.Build.Locator;
using NuGetPush.CLI;
using Serilog;
using Serilog.Events;

MSBuildLocator.RegisterDefaults();

await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(async options =>
{	
	try
	{
		SetProjectFromWorkingDirectory(options);

		var defaults = GetDefaultOptions(options.ProjectDirectory);

		options.LogPath ??= defaults.LogPath;
		options.FeedUrl ??= defaults.FeedUrl ?? throw new Exception("Missing the feed url");
		options.ApiKey ??= defaults.ApiKey ?? throw new Exception("Missing an API key");
		options.PushFromBranch ??= defaults.PushFromBranch;

		Log.Logger = new LoggerConfiguration()
			.WriteTo.File(Path.Combine(options.LogPath, "NuGetPush.log"), LogEventLevel.Information, rollingInterval: RollingInterval.Month, retainedFileCountLimit: 3)
			.WriteTo.Console(LogEventLevel.Information)
			.CreateLogger();		

		var local = GetLocalPackageInfo(options.UsingPostBuildEvent, options.ProjectDirectory, BuildPackage);
		var onlineVersion = await GetOnlinePackageVersionAsync(local.PackageId, options.FeedUrl);

		if (local.Version > onlineVersion)
		{
			Log.Logger.Information(
				"Local version of {packageId} is {localVersion} which is newer than the online version {onlineVersion}", 
				local.PackageId, local.Version, onlineVersion);

			if (!string.IsNullOrWhiteSpace(options.PushFromBranch))
			{
				var gitRepo = FindGetRepository(options.ProjectDirectory);
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
						
			Log.Logger.Information("Publishing new version {version}", local.Version);

			await PushPackageAsync(local.PackageId, options.FeedUrl, options.ApiKey, local.LocalFile);
		}
	}
	catch (Exception exc)
	{
		Log.Logger.Error(exc, "Error in main.");
	}
	
	await Log.CloseAndFlushAsync();
});
