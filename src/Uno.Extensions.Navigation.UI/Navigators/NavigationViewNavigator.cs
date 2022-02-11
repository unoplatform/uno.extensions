using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.UI;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation.Navigators;

public class NavigationViewNavigator : ControlNavigator<Microsoft.UI.Xaml.Controls.NavigationView>
{
	protected override FrameworkElement? CurrentView => Control?.SelectedItem as FrameworkElement;

	public override void ControlInitialize()
	{
		if (Control is not null)
		{
			Control.SelectionChanged += ControlSelectionChanged;
		}
	}

	protected override bool SchemeIsSupported(Route route) =>
		base.SchemeIsSupported(route) ||
		// "../" (change content) Add support for changing current content
		route.IsChangeContent();

	protected override bool CanNavigateToRoute(Route route) =>
		base.CanNavigateToRoute(route) &&
		(FindByPath(RouteResolver.Find(route)?.Path) is not null);

	private void ControlSelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
	{
		var tbi = args.SelectedItem as FrameworkElement;

		var path = tbi?.GetName() ?? tbi?.Name;
		if (path is not null &&
			!string.IsNullOrEmpty(path))
		{
			Region.Navigator()?.NavigateRouteAsync(sender, path, scheme: Schemes.ChangeContent);
		}
	}

	public NavigationViewNavigator(
		ILogger<NavigationViewNavigator> logger,
		IRegion region,
		IRouteResolver routeResolver,
		RegionControlProvider controlProvider)
		: base(logger, region, routeResolver, controlProvider.RegionControl as Microsoft.UI.Xaml.Controls.NavigationView)
	{
	}

	protected override async Task<string?> Show(string? path, Type? viewType, object? data)
	{
		if (Control is null)
		{
			return null;
		}

		Control.SelectionChanged -= ControlSelectionChanged;
		try
		{
			var item = FindByPath(path);
			if (item != null)
			{
				Control.SelectedItem = item;
			}

			// Don't return path, as we need for path to be passed down to children
			return default;
		}
		finally
		{
			await (Control.Content as FrameworkElement).EnsureLoaded();
			Control.SelectionChanged += ControlSelectionChanged;
		}
	}

	private FrameworkElement? FindByPath(string? path)
	{
		if(string.IsNullOrWhiteSpace(path) || Control is null)
		{
			return default;
		}

		var item = (from mi in Control.MenuItems.OfType<FrameworkElement>()
					where (mi.GetName() ?? mi.Name) == path
					select mi).FirstOrDefault();
		return item;
	}
}
