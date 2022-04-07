namespace Uno.Extensions.Navigation;

public interface IRouteNotifier
{
	event EventHandler<RouteChangedEventArgs> RouteChanged;
}
