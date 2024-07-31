namespace TestHarness.Ext.Authentication;

internal interface IAuthenticationRouteInfo
{
	Type LoginViewModel { get;  }
	Type HomeViewModel { get; }
}

internal record AuthenticationRouteInfo(Type LoginViewModel, Type HomeViewModel):IAuthenticationRouteInfo
{
}

internal record AuthenticationRouteInfo<TLoginViewModel, THomeViewModel>() :
	AuthenticationRouteInfo(typeof(TLoginViewModel), typeof(THomeViewModel))
{
}
