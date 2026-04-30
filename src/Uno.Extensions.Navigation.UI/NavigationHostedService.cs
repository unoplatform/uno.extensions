namespace Uno.Extensions.Navigation.UI;

internal class NavigationHostedService : IHostedService, IStartupService
{
	private readonly ILogger<NavigationRegion> _regionLogger;
	private readonly NavigationRouteContext? _routeContext;
	private readonly TaskCompletionSource<bool> _completion = new();

	public NavigationHostedService(
		ILogger<NavigationRegion> regionLogger,
		NavigationRouteContext? routeContext = null)
	{
		_regionLogger = regionLogger;
		_routeContext = routeContext;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		Region.Logger = _regionLogger;

		if (_routeContext is not null)
		{
			NavigationRouteUpdateHandler.Register(_routeContext);
		}

		_completion.SetResult(true);
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		if (_routeContext is not null)
		{
			NavigationRouteUpdateHandler.Unregister(_routeContext);
		}

		return Task.CompletedTask;
	}

	public Task StartupComplete() => _completion.Task;
}
