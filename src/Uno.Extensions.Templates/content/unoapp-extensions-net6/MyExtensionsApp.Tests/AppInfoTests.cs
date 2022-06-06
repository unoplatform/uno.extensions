//-:cnd:noEmit

namespace MyExtensionsApp.Tests;

public class AppInfoTests
{
	[SetUp]
	public void Setup()
	{
	}

	[Test]
	public void AppInfoCreation()
	{
		var appInfo = new AppConfig { Title = "Test" };

		Assert.AreEqual("Test", appInfo.Title);
	}
}
