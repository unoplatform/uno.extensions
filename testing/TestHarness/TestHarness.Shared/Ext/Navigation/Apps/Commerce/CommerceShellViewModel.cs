using TestHarness.Ext.Authentication;

namespace TestHarness.Ext.Navigation.Apps.Commerce;

internal record CommerceShellViewModel
{
	public INavigator Navigator { get; init; }

	private IAuthenticationRouteInfo _routeInfo;


	public CommerceShellViewModel(INavigator navigator, IAuthenticationRouteInfo routeInfo)
	{
		Navigator = navigator;
		_routeInfo = routeInfo;

		_ = Start();
	}

	public async Task Start()
	{
		await Navigator.NavigateViewModelAsync(this, _routeInfo.LoginViewModel, Qualifiers.ClearBackStack);
	}

}
