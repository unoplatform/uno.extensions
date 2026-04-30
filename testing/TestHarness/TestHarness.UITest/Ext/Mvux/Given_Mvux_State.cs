namespace TestHarness.UITest;

public class Given_Mvux_State : NavigationTestBase
{
	[Test]
	public void When_State_Mutated_Via_Command()
	{
		InitTestSection(TestSections.Mvux_Basic);

		App.WaitThenTap("ShowMvuxStatePageButton");

		App.WaitForText("MvuxStateCounterDisplay", "0");

		App.WaitThenTap("MvuxStateIncrementButton");
		App.WaitForText("MvuxStateCounterDisplay", "1");

		App.WaitThenTap("MvuxStateIncrementButton");
		App.WaitForText("MvuxStateCounterDisplay", "2");
	}

	[Test]
	public void When_State_TwoWay_Binding()
	{
		InitTestSection(TestSections.Mvux_Basic);

		App.WaitThenTap("ShowMvuxStatePageButton");

		App.WaitForText("MvuxStateNameDisplay", "Initial Item");

		App.SetText("MvuxStateNameInput", "Updated Name");
		App.WaitForText("MvuxStateNameDisplay", "Updated Name");
	}
}
