
namespace TestHarness.UITest.Ext.Navigation;

public abstract class NavigationTestBase : TestBase
{
	protected void InitTestSection(TestSections section)
	{
		var theListView = App.Marked(TestHarness.Constants.TestSectionsListView);
		App.WaitForElement(theListView);

		theListView.SetDependencyPropertyValue("SelectedIndex", ((int)section).ToString());

		App.WaitForElement(q => q.Marked(TestHarness.Constants.NavigationRoot));

	}
}
