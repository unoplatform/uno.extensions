
namespace TestHarness.UITest.Ext.Navigation;

public abstract class NavigationTestBase : TestBase
{
	protected void InitTestSection(TestSections section)
	{
		// Mobile needs to be full screen to avoid screenshot asserting failures
		// due to the status bar changes like network strength, time, etc
		PlatformHelpers.On(
			iOS: () => App.Tap("TestHarnessMainPageFullScreenButton"),
			Android: () => App.Tap("TestHarnessMainPageFullScreenButton")
		);
		
		var theListView = App.MarkedAnywhere(TestHarness.Constants.TestSectionsListView);
		App.WaitForElement(theListView);

		theListView.SetDependencyPropertyValue("SelectedIndex", ((int)section).ToString());

		App.WaitForElement(q => q.All().Marked(TestHarness.Constants.NavigationRoot));
	}
}
