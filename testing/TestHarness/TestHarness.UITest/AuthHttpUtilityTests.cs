using System.Web;

namespace TestHarness.UITest;

[TestFixture]
public class AuthHttpUtilityTests 
{
	[Test]
	public void ParseQueryStringTest()
	{
		var query = AuthHttpUtility.ExtractArguments("myapp:///#access_token=somelongtoken&expires=1662391139");
		query.Should().NotBeNull();
		query["access_token"].Should().Be("somelongtoken");
	}
}
