namespace Uno.Extensions.Navigation;

internal interface IResponseNavigator : INavigator
{
	NavigationResponse AsResponseWithResult(NavigationResponse? response);
}

internal interface IResponseNavigatorFactory
{
	IResponseNavigator CreateForResultType<TResult>(INavigator navigator, NavigationRequest request);
}
