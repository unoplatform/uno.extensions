using System.Diagnostics;
using Uno.Extensions.Logging;

namespace Uno.Extensions.Navigation;

public class RouteNotifier : IRouteNotifier, IRouteUpdater
{
	public event EventHandler<RouteChangedEventArgs>? RouteChanged;

	private ILogger Logger { get; }
	public RouteNotifier(ILogger<RouteNotifier> logger)
	{
		Logger = logger;
	}

	private IDictionary<Guid, IRegion> regionRoots= new Dictionary<Guid, IRegion>();
	private IDictionary<IRegion, int> runningNavigations = new Dictionary<IRegion, int>();
	private IDictionary<IRegion, Stopwatch> timers = new Dictionary<IRegion, Stopwatch>();

	public Guid StartNavigation(IRegion region)
	{
		var regionUpdateId = Guid.NewGuid();
		var root = region.Root();
		regionRoots[regionUpdateId] = root;

		if (!runningNavigations.TryGetValue(root, out var count) ||
			count == 0)
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Pre-navigation: - {root.ToString()}");
			runningNavigations[root] = 1;
			var timer = new Stopwatch();
			timers[root] = timer;
			timer.Start();
		}
		else
		{
			runningNavigations[root] = runningNavigations[root] + 1;
			timers[root].Restart();
		}

		return regionUpdateId;
	}

	public void EndNavigation(Guid regionUpdateId)
	{
		var root= regionRoots[regionUpdateId];
		regionRoots.Remove(regionUpdateId);
		runningNavigations[root] = runningNavigations[root] - 1;

		if (runningNavigations[root] == 0)
		{
			timers[root].Stop();
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Elapsed navigation: {timers[root].ElapsedMilliseconds}");
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Post-navigation: {root.ToString()}");
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Post-navigation (route): {root.GetRoute()}");

			RouteChanged?.Invoke(this, new RouteChangedEventArgs(root));
		}
	}
}

