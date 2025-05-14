using TestHarness.Ext.Navigation.Apps.Chefs.Models;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public partial class ChefsLoginModel
{
	private readonly INavigator _navigator;

	public ChefsLoginModel(
		INavigator navigator)
	{
		_navigator = navigator;
	}


	public async ValueTask DoLogin()
	{
		await _navigator.NavigateViewModelAsync<ChefsRootModel>(this, Qualifiers.ClearBackStack);
	}
}
