namespace TestHarness.UITest;

public class Given_Oidc : NavigationTestBase
{
	[Test]
	public async Task When_Oidc_Auth()
	{
		InitTestSection(TestSections.Authentication_Oidc);

		App.WaitThenTap("ShowAppButton");

		App.WaitElement("LoginNavigationBar");
		var text = App.GetText("LoginProviderCountText");
		Assert.AreEqual("4", text);
	}

}
