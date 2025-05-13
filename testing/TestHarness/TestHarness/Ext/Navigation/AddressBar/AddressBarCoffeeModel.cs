using Uno.Extensions.Navigation.Navigators;

namespace TestHarness.Ext.Navigation.AddressBar;

public partial class AddressBarCoffeModel
{
	public AddressBarCoffeModel(INavigator navigator, IRouteNotifier notifier)
	{
		notifier.RouteChanged += RouteChanged;
	}

	private async void RouteChanged(object? sender, RouteChangedEventArgs e)
	{
		// Prints the right route
		var currentRoute = e.Navigator.Route.FullPath();
	}
}
