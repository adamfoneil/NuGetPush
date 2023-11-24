using CommandLine;

namespace NuGetPush.CLI;

internal class Options
{
	[Option('p', "ProjectPath", Default = ".")]
	public string ProjectPath { get; set; } = default!;

	[Option('k', "ApiKey")]
	public string ApiKey { get; set; } = default!;

	[Option('f', "FeedUrl")]
	public string FeedUrl { get; set; } = "https://api.nuget.org/v3/index.json";

	[Option('l', "LogPath")]
	public string LogPath { get; set; } = "%localappdata%\\NuGetPush";
}
