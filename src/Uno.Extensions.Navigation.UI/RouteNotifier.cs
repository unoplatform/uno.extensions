using System.Diagnostics;
using Uno.Extensions.Diagnostics;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

internal class RouteNotifier : IRouteNotifier, IRouteUpdater
{
	public event EventHandler<RouteChangedEventArgs>? RouteChanged;

	private ILogger Logger { get; }
	public RouteNotifier(ILogger<RouteNotifier> logger)
	{
		Logger = logger;
	}

	private IDictionary<Guid, StringBuilder> navigationSegments = new Dictionary<Guid, StringBuilder>();
	private IDictionary<Guid, int> runningNavigations = new Dictionary<Guid, int>();
	private IDictionary<Guid, IRegion> rootRegions = new Dictionary<Guid, IRegion>();


	public void StartNavigation(INavigator navigator, IRegion region, NavigationRequest request)
	{
		var id = request.Id;
		if (!runningNavigations.TryGetValue(id, out var count) ||
			count == 0)
		{
			runningNavigations[id] = 1;
			// Capture the root region now, while the region hierarchy is
			// still intact. By the time EndNavigation fires, the originating
			// region may have been detached from the tree (e.g. its parent
			// page was unloaded during Frame navigation), which would cause
			// region.Root() to return the detached region instead of the
			// app root.
			rootRegions[id] = region.Root();
			navigationSegments[id] = new StringBuilder();
			navigationSegments[id].AppendLine($"[{id}] Navigation Start");
			PerformanceTimer.Start(Logger, LogLevel.Trace, id);
		}
		else
		{
			runningNavigations[id] = runningNavigations[id] + 1;
		}
		if (Logger.IsEnabled(LogLevel.Trace))
		{
			navigationSegments[id].AppendLine($"[{id} - {PerformanceTimer.Split(id).TotalMilliseconds}] {navigator.GetType().Name} - {region.Name ?? "unnamed"} - {request.Route} {(request.Route.IsInternal ? "(internal)" : "")}");
		}
	}

	public void EndNavigation(INavigator navigator, IRegion region, NavigationRequest request, NavigationResponse? response)
	{
		var id = request.Id;
		runningNavigations[id] = runningNavigations[id] - 1;

		if (runningNavigations[id] == 0)
		{
			var elapsed = PerformanceTimer.Stop(Logger, LogLevel.Trace, id);
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				navigationSegments[id].AppendLine($"[{id} - {elapsed.TotalMilliseconds}] Navigation End");
				Logger.LogTraceMessage($"Post-navigation (summary):\n{navigationSegments[id]}");
			}
			navigationSegments.Remove(id);
			// Use the root region captured at StartNavigation time to
			// avoid reading a stale/detached hierarchy.
			var rootRegion = rootRegions[id];
			rootRegions.Remove(id);
			var route = rootRegion.GetRoute();
			RouteChanged?.Invoke(this, new RouteChangedEventArgs(rootRegion, response?.Navigator, route));
		}
	}
}

