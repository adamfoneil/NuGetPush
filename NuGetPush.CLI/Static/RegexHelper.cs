using System.Text.RegularExpressions;

namespace NuGetPush.CLI.Static;

public static class RegexHelper
{
	public static string ParsePackageId(string expression)
	{
		var match = Regex.Match(expression, @".(\d+).(\d+).(\d+)");
		var index = match.Success ? match.Index : -1;
		return (index > -1) ? expression[0..index] : expression;
	}		
}
