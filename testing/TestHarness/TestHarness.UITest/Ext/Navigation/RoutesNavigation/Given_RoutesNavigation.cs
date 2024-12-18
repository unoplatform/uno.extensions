namespace TestHarness.UITest;

public class Given_RoutesNavigation : NavigationTestBase
{
	[Test]
	public async Task When_Navigate_To_SamplePage()
	{
		InitTestSection(TestSections.Navigation_RoutesNavigation);

		App.WaitThenTap("SamplePageButton");

		App.WaitElement("SamplePage");
	}

	[Test]
	public async Task When_Navigate_To_SecondPage()
	{
		InitTestSection(TestSections.Navigation_RoutesNavigation);

		App.WaitThenTap("SecondPageButton");

		App.WaitElement("SecondPage");
	}

	[Test]
	public async Task When_Navigate_To_Unregistered_SecondPage()
	{
		InitTestSection(TestSections.Navigation_RoutesNavigation);

		App.WaitThenTap("SecondPageUnregisteredButton");

		App.WaitElement("SecondPage");
	}

	[Test]
	public async Task When_Navigate_To_List_Template()
	{
		InitTestSection(TestSections.Navigation_RoutesNavigation);

		App.WaitThenTap("ListTemplateButton");

		App.WaitElement("ListTemplate");
	}

	[Test]
	public async Task When_Navigate_To_ItemsPage()
	{
		InitTestSection(TestSections.Navigation_RoutesNavigation);

		App.WaitThenTap("ItemsPageButton");

		App.WaitElement("ItemsPage");
	}

	[Test]
	public async Task When_Navigate_To_MyControl()
	{
		InitTestSection(TestSections.Navigation_RoutesNavigation);

		App.WaitThenTap("MyControlButton");

		App.WaitElement("MyControlView");
	}

	[Test]
	public async Task When_Navigate_To_MyControlView()
	{
		InitTestSection(TestSections.Navigation_RoutesNavigation);

		App.WaitThenTap("MyControlViewButton");

		App.WaitElement("MyControlView");
	}
}
