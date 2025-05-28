using TestHarness.Ext.Navigation.Apps.Chefs.Models;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public partial class ChefsSearchModel
{
	public ChefsSearchModel(INavigator navigator, ChefsSearchFilter filter)
	{
		_navigator = navigator;
		_filter = filter;
	}

	private INavigator _navigator;
	private ChefsSearchFilter _filter;

	public async ValueTask NavigateToRecipeDetail()
	{
		await _navigator.NavigateRouteAsync(this, "ChefsRecipeDetails", data: new ChefsRecipe { Name = "Search" });
	}
}
