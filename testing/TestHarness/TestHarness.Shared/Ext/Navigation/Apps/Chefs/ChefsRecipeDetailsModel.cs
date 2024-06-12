using TestHarness.Ext.Navigation.Apps.Chefs.Models;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public partial class ChefsRecipeDetailsModel(ChefsRecipe recipe)
{
	public ChefsRecipe Recipe { get; } = recipe;
}
