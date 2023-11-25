using CommandLine;

namespace NuGetPush.CLI;

internal class Options
{
	[Option('p', "ProjectDir")]
	public string ProjectDirectory { get; set; } = default!;

	[Option('k', "ApiKey")]
	public string ApiKey { get; set; } = default!;

	[Option('f', "FeedUrl")]
	public string FeedUrl { get; set; } = default!;

	[Option('l', "LogPath")]
	public string LogPath { get; set; } = default!;

	/// <summary>
	/// push only when this branch is checked out
	/// </summary>
	[Option('b', "PushFromBranch")]
	public string PushFromBranch { get; set; } = default!;
}
