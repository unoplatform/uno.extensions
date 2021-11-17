using System;

namespace Uno.Extensions.Navigation;

public class RouteNotifier : IRouteNotifier, IRouteUpdater
{
	public event EventHandler? RouteChanged;

	private int runningNavigations;

	public void StartNavigation()
	{
		runningNavigations++;
	}

	public void EndNavigation()
	{
		runningNavigations--;
		if (runningNavigations == 0)
		{
			RouteChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}

