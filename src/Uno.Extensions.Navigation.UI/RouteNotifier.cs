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

	private IDictionary<IRegion, int> runningNavigations = new Dictionary<IRegion, int>();
	private IDictionary<IRegion, Stopwatch> timers = new Dictionary<IRegion, Stopwatch>();

	public void StartNavigation(IRegion region)
	{
		region = region.Root();

		if (!runningNavigations.TryGetValue(region, out var count) ||
			count == 0)
		{
			runningNavigations[region] = 1;
			var timer = new Stopwatch();
			timers[region] = timer;
			timer.Start();
		}
		else
		{
			runningNavigations[region] = runningNavigations[region] + 1;
			timers[region].Restart();
		}
	}

	public void EndNavigation(IRegion region)
	{
		region = region.Root();
		runningNavigations[region] = runningNavigations[region] - 1;

		if (runningNavigations[region] == 0)
		{
			timers[region].Stop();
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Elapsed navigation: {timers[region].ElapsedMilliseconds}");
			RouteChanged?.Invoke(this, new RouteChangedEventArgs(region));
		}
	}
}

