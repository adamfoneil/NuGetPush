namespace NuGetPush.CLI;

internal class Options
{	
	public string ApiKey { get; set; } = default!;
	public string FeedUrl { get; set; } = "https://api.nuget.org/v3/index.json";
	public string SymbolFeedUrl { get; set; } = "https://symbols.nuget.org";
	public string LogPath { get; set; } = "%appdata%\\NuGetPush";
}
