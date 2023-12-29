using NuGetPush.CLI.Static;

namespace Tests;

[TestClass]
public class MainMethods
{
	[TestMethod]
	[DataRow("This.Package.1.2.3", "This.Package")]
	[DataRow("That.Package.2.2.0-alpha", "That.Package")]
	[DataRow("Dapper.Entities.PostgreSql.8.0.0-alpha", "Dapper.Entities.PostgreSql")]
	public void ParsePackageId(string input, string expected)
	{
		var actual = RegexHelper.ParsePackageId(input);
		Assert.AreEqual(expected, actual);
	}
}