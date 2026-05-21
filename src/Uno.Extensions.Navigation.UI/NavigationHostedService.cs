namespace Uno.Extensions.Navigation.UI;

internal class NavigationHostedService : IHostedService, IStartupService
{
	private readonly ILogger<NavigationRegion> _regionLogger;
	private readonly NavigationRouteContext? _routeContext;
	private readonly IRouteResolver? _routeResolver;
	private readonly TaskCompletionSource<bool> _completion = new();

	public NavigationHostedService(
		ILogger<NavigationRegion> regionLogger,
		NavigationRouteContext? routeContext = null,
		IRouteResolver? routeResolver = null)
	{
		_regionLogger = regionLogger;
		_routeContext = routeContext;
		_routeResolver = routeResolver;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		Region.Logger = _regionLogger;

		if (_routeContext is not null)
		{
			// HostBuilderExtensions.UseNavigation registers IRouteResolver
			// directly with MappedRouteResolver, which overrides the factory
			// delegate in ServiceCollectionExtensions.AddNavigation that used
			// to assign ctx.Resolver. Without this assignment, every HR
			// UpdateApplication invocation bails at "resolver is null" and the
			// route table never rebuilds. Assigning here closes that gap: by
			// the time StartAsync runs, both NavigationRouteContext and
			// IRouteResolver have been resolved from the same scope, so we can
			// link the two without dragging the dependency into the resolver
			// factory.
			if (_routeContext.Resolver is null && _routeResolver is RouteResolver rr)
			{
				_routeContext.Resolver = rr;
			}

			NavigationRouteUpdateHandler.Register(_routeContext);
		}

		if (_regionLogger.IsEnabled(LogLevel.Information))
		{
			_regionLogger.LogInformationMessage($"Navigation engine started (hot-reload route refresh: {(_routeContext is not null ? "enabled" : "disabled")}, resolver: {_routeResolver?.GetType().Name ?? "<none>"})");
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

		if (_regionLogger.IsEnabled(LogLevel.Information))
		{
			_regionLogger.LogInformationMessage("Navigation engine stopped");
		}

		return Task.CompletedTask;
	}

	public Task StartupComplete() => _completion.Task;
}
