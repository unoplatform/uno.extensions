using TestHarness.Ext.Navigation.Apps.Chefs.Models;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public partial class ChefsSearchModel
{
	private INavigator _navigator;
	public ChefsSearchFilter Filter { get; init; }
	public ChefsSearchModel(INavigator navigator, ChefsSearchFilter? filter = default)
	{
		_navigator = navigator;
		Filter = filter ?? new ChefsSearchFilter { Category = "Breakfast" };
	}
	public async ValueTask ShowRecipe()
	{
		await _navigator.NavigateRouteAsync(this, "ChefsSearchRecipeDetails");
	}
}
