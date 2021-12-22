namespace Uno.Extensions.Navigation;

internal interface IResponseNavigator : INavigator
{
	NavigationResponse AsResponseWithResult(NavigationResponse? response);
}
