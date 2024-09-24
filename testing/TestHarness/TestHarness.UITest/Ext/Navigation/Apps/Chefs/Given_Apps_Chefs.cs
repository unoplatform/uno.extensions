namespace TestHarness.UITest;

public class Given_Apps_Chefs : NavigationTestBase
{
	[Test]
	public async Task When_Chefs_FavoriteRecipes_RecipeDetails()
	{
		InitTestSection(TestSections.Apps_Chefs);
		
		App.WaitThenTap("ShowAppButton");
		App.WaitThenTap("NextButton");
		await Task.Delay(5000);
		App.WaitThenTap("LoginButton");
		await Task.Delay(5000);
		App.WaitThenTap("FavoriteRecipesButton");
		await Task.Delay(5000);
		App.WaitThenTap("RecipeDetailsButton");
	}
}
