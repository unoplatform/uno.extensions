using NUnit.Framework;
using Query = System.Func<Uno.UITest.IAppQuery, Uno.UITest.IAppQuery>;

namespace MyExtensionsApp.UITests;

public class Given_MainPage : TestBase
{
	[Test]
	public void When_SmokeTest()
	{

		// Make sure the ViewModelButton has rendered
		App.WaitForElement(q => q.Marked("ViewModelButton"));

		// Query for the XamlButton and then tap it
		Query xamlButton = q => q.All().Marked("XamlButton");
		App.WaitForElement(xamlButton);
		App.Tap(xamlButton);

		// Take a screenshot and add it to the test results
		TakeScreenshot("After tapped");
	}
}
