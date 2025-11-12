using System.Diagnostics.CodeAnalysis;

using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation.Toolkit.Navigators;

public class DrawerControlNavigator : ControlNavigator<DrawerControl>
{
	protected override FrameworkElement? CurrentView => _content;
	private FrameworkElement? _content;

	public DrawerControlNavigator(
		ILogger<ContentControlNavigator> logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver,
		RegionControlProvider controlProvider)
		: base(logger, dispatcher, region, resolver, controlProvider.RegionControl as DrawerControl)
	{
	}

	public override void ControlInitialize()
	{
		base.ControlInitialize();

		//if (Control is not null)
		//{
		//    Control.Closed += Control_Closed;
		//}
	}

	private void Control_Closed(object? sender, object e)
	{
		Region.Navigator()?.NavigateRouteAsync((sender ?? Control) ?? this, "hide");
	}

	protected override Task<bool> RegionCanNavigate(Route route, RouteInfo? routeMap)
	{
		if (route.IsBackOrCloseNavigation())
		{
			return Task.FromResult(true);
		}
		return base.RegionCanNavigate(route, routeMap);
	}

	protected override async Task<string?> Show(
		string? path,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type? viewType,
		object? data)
	{
		if (Control is null)
		{
			return string.Empty;
		}

		try
		{
			Control.IsOpen = !(path?.StartsWith(Qualifiers.NavigateBack) ?? false);
			_content = Control?.Content as FrameworkElement;
			return path;
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Error)) Logger.LogErrorMessage($"Unable to create instance - {ex.Message}");
		}

		return default;
	}


	protected override async Task<Route?> ExecuteRequestAsync(NavigationRequest request)
	{
		if (Region.Services is null || Control is null)
		{
			return default;
		}

		var route = request.Route;
		if (route.FrameIsBackNavigation())
		{
			CloseDrawer();
		}
		else
		{
			Control.IsOpen = true;
			_content = Control?.Content as FrameworkElement;
		}
		var responseRequest = route with { Path = null };
		return responseRequest;
	}

	private void CloseDrawer()
	{
		if (Control?.IsOpen ?? false)
		{
			Control.IsOpen = false;
		}
	}

	protected override Task CheckLoadedAsync() => _content is not null ? _content.EnsureLoaded() : Task.CompletedTask;
}
