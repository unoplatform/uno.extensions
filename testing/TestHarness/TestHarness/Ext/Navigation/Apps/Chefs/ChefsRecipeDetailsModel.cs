using System.Collections.Immutable;
using TestHarness.Ext.Navigation.Apps.Chefs.Models;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public partial class ChefsRecipeDetailsModel(ChefsRecipe recipe, INavigator navigator)
{
	public ChefsRecipe Recipe { get; } = recipe;

	private readonly INavigator _navigator = navigator;

	public async ValueTask LiveCooking()
	{
		await _navigator.NavigateRouteAsync(this, "ChefsLiveCooking", data: new ChefsLiveCookingParameter(Recipe, ImmutableList<ChefsStep>.Empty));
	}
}
