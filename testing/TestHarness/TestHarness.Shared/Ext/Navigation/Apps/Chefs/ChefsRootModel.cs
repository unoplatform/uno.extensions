using TestHarness.Ext.Navigation.Apps.Chefs.Models;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public partial class ChefsRootModel(ChefsCredentials credentials, INavigator navigator)
{
	public ChefsCredentials Credentials { get; } = credentials;
}
