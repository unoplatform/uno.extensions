using NUnit.Framework;

namespace TestHarness.UITests;

public class Given_MessageDialog : TestBase
{
	[Test]
	public void When_MessageDialog()
	{
		App.WaitForElement(q => q.Marked("TestHarnessMainPageTitle"));

		 // TODO: Select PageNavigation from dropdown - this will nav to PageNavigationMainPage

		// How to select item from combobox (perhaps use listview?)

		// How to detect if a message dialog is visible
	}
}
