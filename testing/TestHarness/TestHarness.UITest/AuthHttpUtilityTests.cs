using System.Web;

namespace TestHarness.UITest;

[TestFixture]
public class AuthHttpUtilityTests 
{
	[Test]
	public void ParseQueryStringTest()
	{
		var query = AuthHttpUtility.ParseQueryString("oneflow:///#access_token=ya29.A0AVA9y1vvvOaAvbrJETvgi-FIo319q_838N25J0KVQA0_1msfgsfgsfdgsdfgsfgfgfgfgfgfg6Bh9hBbpEwe5MfvE2B4KiKDTCczr-GN6ev5ag2urmjeky4byY1BDpgogjdDJWBCzLg6Q8Iq4MmsUHwjdo0NCogFPPfeMsaCgYKATASATASFQE65dr8MjfcAYCTV3XcVAwae9SrYw0166&expires=1662391139");
	}
}
