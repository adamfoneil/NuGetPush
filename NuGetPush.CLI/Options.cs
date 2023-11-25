using CommandLine;

namespace NuGetPush.CLI;

internal class Options
{
	[Option('p', "ProjectDir")]
	public string ProjectDirectory { get; set; } = default!;

	[Option('k', "ApiKey")]
	public string ApiKey { get; set; } = default!;

	[Option('f', "FeedUrl")]
	public string FeedUrl { get; set; } = "https://api.nuget.org/v3/index.json";

	[Option('l', "LogPath")]
	public string LogPath { get; set; } = default!;

	/// <summary>
	/// push only when this branch is checked out
	/// </summary>
	[Option('b', "PushFromBranch")]
	public string PushFromBranch { get; set; } = default!;

	[Option('e', "UsingPostBuildEvent", HelpText = "Indicates that this was invoked from the Post Build event, which causes packages to be rebuilt internally")]
	public bool UsingPostBuildEvent { get; set; }
}
