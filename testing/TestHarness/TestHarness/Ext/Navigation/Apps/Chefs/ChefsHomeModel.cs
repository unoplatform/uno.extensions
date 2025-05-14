using TestHarness.Ext.Navigation.Apps.Chefs.Models;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public partial class ChefsHomeModel(INavigator navigator)
{
	public async ValueTask NavigateToRecipeDetail()
	{
		await navigator.NavigateRouteAsync(this, "ChefsRecipeDetails", data: new ChefsRecipe { Name = "Home" });
	}

	public async ValueTask NavigateToFavoriteRecipes()
	{
		await navigator.NavigateRouteAsync(this, "ChefsFavoriteRecipes");
	}

	public async ValueTask NavigateToSearch(CancellationToken ct) =>
		await navigator.NavigateViewModelAsync<ChefsSearchModel>(this, data: new ChefsSearchFilter(), cancellation: ct);
}
