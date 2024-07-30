using TestHarness.Ext.Authentication;

namespace TestHarness.Ext.Navigation.Apps.Commerce;

internal record CommerceLoginViewModel(INavigator Navigator, IAuthenticationRouteInfo RouteInfo)
{
	public async void Login()
	{
			await Navigator.NavigateViewModelAsync(this, RouteInfo.HomeViewModel, qualifier: Qualifiers.ClearBackStack);
	}

}
