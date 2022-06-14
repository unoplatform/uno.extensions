
namespace TestHarness.UITest.Ext.Navigation;

public abstract class NavigationTestBase : TestBase
{
	protected void InitTestSection(TestSections section)
	{
		var theComboBox = App.Marked(TestHarness.Constants.TestSectionsComboBox);
		App.WaitForElement(theComboBox);

		theComboBox.SetDependencyPropertyValue("SelectedIndex", ((int)section).ToString());

		App.WaitForElement(q => q.Marked(TestHarness.Constants.NavigationRoot));

	}
}
