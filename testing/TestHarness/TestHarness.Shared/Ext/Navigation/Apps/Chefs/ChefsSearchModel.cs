using TestHarness.Ext.Navigation.Apps.Chefs.Models;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public partial class ChefsSearchModel(INavigator navigator)
{
	public async ValueTask NavigateToRecipeDetail()
	{
		await navigator.NavigateRouteAsync(this, "ChefsSearchRecipeDetails", data: new ChefsRecipe { Name = "Search Page" });
	}
}
