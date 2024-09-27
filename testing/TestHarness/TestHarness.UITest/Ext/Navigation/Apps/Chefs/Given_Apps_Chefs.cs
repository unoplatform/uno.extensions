namespace TestHarness.UITest;

public class Given_Apps_Chefs : NavigationTestBase
{
	[Test]
	public async Task When_Chefs_FavoriteRecipes_RecipeDetails()
	{
		InitTestSection(TestSections.Apps_Chefs);
		
		App.WaitThenTap("ShowAppButton");
		App.WaitThenTap("NextButton");
		await Task.Delay(10000);
		App.WaitThenTap("LoginButton", timeout: TimeSpan.FromSeconds(10));
		App.WaitThenTap("FavoriteRecipesButton");
		App.WaitThenTap("RecipeDetailsButton");
	}
}
