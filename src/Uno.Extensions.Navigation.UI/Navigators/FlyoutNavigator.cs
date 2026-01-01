using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions.Navigation.Navigators;

public class FlyoutNavigator : ClosableNavigator
{
	public override bool CanGoBack => _flyout is not null;

	private Flyout? _flyout;
	private FrameworkElement? _content;

	private Window _window;

	public FlyoutNavigator(
		ILogger<ContentDialogNavigator> logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver,
		Window window)
		: base(logger, dispatcher, region, resolver)
	{
		_window = window;
	}

	protected override Task<bool> RegionCanNavigate(Route route, RouteInfo? routeMap)
	{
		if (route.IsBackOrCloseNavigation())
		{
			return Task.FromResult(true);
		}
		if (routeMap?.RenderView is null)
		{
			return Task.FromResult(false);
		}
		return base.RegionCanNavigate(route, routeMap);
	}

	protected override async Task<Route?> ExecuteRequestAsync(NavigationRequest request)
	{
		// Capture the Source - return value can be ignored
		_ = await base.ExecuteRequestAsync(request);

		if (Region.Services is null)
		{
			return default;
		}

		var route = request.Route;
		// If this is back navigation, then make sure it's used to close
		// any of the open dialogs
		var injectedFlyout = false;
		if (route.FrameIsBackNavigation())
		{
			CloseFlyout();
		}
		else
		{
			if (_flyout is not null)
			{
				return Route.Empty;
			}

			var mapping = Resolver.FindByPath(route.Base);
			injectedFlyout = !(mapping?.RenderView?.IsSubclassOf(typeof(Flyout)) ?? false);
			var viewModel = await CreateViewModel(Region.Services, request, route, mapping);
			_flyout = await DisplayFlyout(request, mapping?.RenderView, viewModel, injectedFlyout);

			if (request.Cancellation.HasValue &&
				request.Cancellation.Value.CanBeCanceled)
			{
				request.Cancellation.Value.Register(async () =>
				{
					await this.Dispatcher.ExecuteAsync(() => CloseFlyout());
				});
			}
		}
		var responseRequest = injectedFlyout ? Route.Empty : route with { Path = null };
		return responseRequest;
	}

	private void CloseFlyout()
	{
		if (_flyout is { } fly)
		{
			CleanupFlyout();

			fly.Hide();

		}

	}

	private async Task<Flyout?> DisplayFlyout(NavigationRequest request, Type? viewType, object? viewModel, bool injectedFlyout)
	{
		var route = request.Route;
		var navigation = Region.Navigator();
		var services = Region.Services;

		if (navigation is null ||
			services is null)
		{
			return default;
		}

		Flyout? flyout = null;
		if (viewType is not null)
		{
			if (!injectedFlyout)
			{
				flyout = viewType.CreateInstance<Flyout>(Logger);
			}
			else
			{
				flyout = services.GetService<Flyout>();
			}
		}
		else
		{
			object? resource = request.RouteResourceView(Region);
			flyout = resource as Flyout;
		}

		if (flyout is null)
		{
			return default;
		}

		var flyoutElement = flyout.Content as FrameworkElement;
		if (flyoutElement is not null)
		{
			if (!injectedFlyout)
			{
				await flyoutElement.InjectServicesAndSetDataContextAsync(services, navigation, viewModel);
			}

			flyoutElement.SetInstance(Region);
			if (!injectedFlyout)
			{
				flyoutElement.SetName(route.Base); // Set the name on the flyout element
			}
			_content = flyoutElement;
		}

		var flyoutHost = request.Sender as FrameworkElement;
		if (flyoutHost is null)
		{
			flyoutHost = Region.View;

			if (flyoutHost is null)
			{
				flyoutHost = _window.Content as FrameworkElement;
			}
		}

		flyout.ShowAt(flyoutHost);

		flyout.Closed += Flyout_Closed;


		if (flyoutElement is not null && flyoutElement.Parent is not null)
		{
			flyoutElement.SetInstance(null); // Clear region off the flyout element
			flyoutElement.Parent.SetInstance(Region); // Set region on parent (now that it will be not null)
			flyoutElement.ReassignRegionParent(); // Update any sub-regions to correct their relationship with the parent (ie set the name)
		}

		return flyout;
	}

	private async void Flyout_Closed(object? sender, object e)
	{
		if (_flyout is null)
		{
			return;
		}

		CleanupFlyout();

		var navigation = Region.Navigator();
		if (navigation is null)
		{
			return;
		}

		await navigation.NavigateBackAsync(this);
	}

	private void CleanupFlyout()
	{
		if (_flyout is null)
		{
			return;
		}

		_flyout.Closed -= Flyout_Closed;
		_flyout = null;
		_content = null;

	}

	protected override Task CheckLoadedAsync() => _flyout is not null && _content is not null ? _content.EnsureLoaded() : Task.CompletedTask;

	protected override async Task CloseNavigator() => CloseFlyout();

}
