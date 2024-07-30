namespace TestHarness.UITest;

[TestFixture]
public class AuthHttpUtilityTests
{
	[Test]
	[NUnit.Framework.Ignore("Removed to avoid direct dependency on Uno.Extensions libraries")]
	public void ParseQueryStringTest()
	{
		//var query = AuthHttpUtility.ExtractArguments("myapp:///#access_token=somelongtoken&expires=1662391139");
		//query.Should().NotBeNull();
		//query["access_token"].Should().Be("somelongtoken");
	}
}
