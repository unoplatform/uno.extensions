using TestHarness.Ext.Navigation.Apps.Chefs.Models;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public partial class ChefsProfileModel
{
	public ChefsRecipe Recipe => new ChefsRecipe { Name = "Test Recipe" };
}
