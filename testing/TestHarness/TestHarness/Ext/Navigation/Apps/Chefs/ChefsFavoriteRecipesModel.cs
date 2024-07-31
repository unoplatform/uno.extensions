using TestHarness.Ext.Navigation.Apps.Chefs.Models;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public partial class ChefsFavoriteRecipesModel(INavigator navigator)
{
	public async ValueTask NavigateToRecipeDetail()
	{
		await navigator.NavigateRouteAsync(this, "ChefsFavoriteRecipeDetails", data: new ChefsRecipe { Name = "Favorite Page" });
	}
}
