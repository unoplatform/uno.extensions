using TestHarness.Ext.Navigation.Apps.Chefs.Models;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public partial class ChefsLiveCookingModel
{
	public ChefsRecipe Recipe { get; }

	public ChefsLiveCookingModel(ChefsLiveCookingParameter parameter)
	{
		Recipe = parameter.Recipe;
	}
}
