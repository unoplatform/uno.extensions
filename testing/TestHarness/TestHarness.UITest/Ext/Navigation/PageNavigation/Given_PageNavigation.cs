namespace TestHarness.UITests;

public class Given_PageNavigation : NavigationTestBase
{
	[Test]
	public void When_PageNavigation()
	{
		InitTestSection(TestSections.PageNavigation);

		//App.WaitForElement(q => q.Marked("TestHarnessMainPageTitle"));


		//App.WaitForElement(App.Marked("TestSectionsComboBox"));
		//var theComboBox = App.Marked("TestSectionsComboBox");

		//theComboBox.SetDependencyPropertyValue("SelectedIndex", "0");

		//App.WaitForElement(q => q.Marked("PageNavigationMainText"));

	}
}
