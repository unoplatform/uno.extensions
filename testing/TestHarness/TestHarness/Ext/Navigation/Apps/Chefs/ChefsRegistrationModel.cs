using TestHarness.Ext.Navigation.Apps.Chefs.Models;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public partial class ChefsRegistrationModel
{
	private readonly INavigator _navigator;

	public ChefsRegistrationModel(
		INavigator navigator)
	{
		_navigator = navigator;
	}

	public async ValueTask Register()
	{
		await _navigator.NavigateViewModelAsync<ChefsRootModel>(this, Qualifiers.ClearBackStack, new ChefsCredentials { Username = "Tester" });
	}
}
