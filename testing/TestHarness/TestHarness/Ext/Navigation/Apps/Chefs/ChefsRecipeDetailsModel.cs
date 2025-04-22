using System.Collections.Immutable;
using TestHarness.Ext.Navigation.Apps.Chefs.Models;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public partial class ChefsRecipeDetailsModel(ChefsRecipe recipe, INavigator navigator)
{
	public ChefsRecipe Recipe { get; } = recipe;

	private readonly INavigator _navigator = navigator;

	public async ValueTask LiveCooking()
	{
		var route = _navigator?.Route?.Base switch
		{
			_ => "ChefsLiveCooking"
		};

		await _navigator.NavigateDataAsync(this, new ChefsLiveCookingParameter(Recipe, ImmutableList<ChefsStep>.Empty));
	}
}
