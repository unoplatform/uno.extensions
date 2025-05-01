using TestHarness.Ext.Navigation.Apps.Chefs.Models;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public partial class ChefsHomeModel(INavigator navigator)
{

	public async ValueTask ShowCurrentProfile()
	{
		await navigator.NavigateToProfile(this, new ChefsUser { FullName = "Tester User" });
	}

	public async ValueTask ShowCurrentProfileNew()
	{
		await navigator.NavigateRouteAsync(this, "ChefsProfile", qualifier: Qualifiers.Dialog, data: new ChefsUser { FullName = "Tester User (new)" });
	}

	public async ValueTask NavigateToRecipeDetail()
	{
		await navigator.NavigateRouteAsync(this, "ChefsRecipeDetails", data: new ChefsRecipe { Name = "Home" });
	}

	public async ValueTask NavigateToFavoriteRecipes()
	{
		await navigator.NavigateRouteAsync(this, "ChefsFavoriteRecipes");
	}
}

public static class ChefsNavigationExtensions
{
	public static async ValueTask NavigateToProfile(this INavigator navigator, object sender, ChefsUser? profile = null)
	{
		var response = await navigator.NavigateRouteForResultAsync<ChefsRecipe?>(sender, "ChefsProfile", qualifier: Qualifiers.Dialog, data: profile);
		var result = await response!.Result;

		//If a Recipe was selected, navigate to the RecipeDetails. Otherwise, do nothing
		await (result.SomeOrDefault() switch
		{
			ChefsRecipe recipe => navigator.NavigateDataAsync(sender, recipe),
			_ => Task.CompletedTask,
		});
	}
}
