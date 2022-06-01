using NUnit.Framework;

namespace TestHarness.UITests;

public class Given_MainPage : TestBase
{
	[Test]
	public void When_SmokeTest()
	{
		App.WaitForElement(q => q.Marked("TestHarnessMainPageTitle"));
	}
}
