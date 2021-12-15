namespace Uno.Extensions.Navigation;

internal interface IResponseNavigatorFactory
{
	IResponseNavigator CreateForResultType<TResult>(INavigator navigator, NavigationRequest request);
}
