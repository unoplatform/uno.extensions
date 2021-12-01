namespace Uno.Extensions.Navigation;

internal class ResponseNavigatorFactory : IResponseNavigatorFactory
{
	public IResponseNavigator CreateForResultType<TResult>(INavigator navigator, NavigationRequest request) => new ResponseNavigator<TResult>(navigator, request);
}
