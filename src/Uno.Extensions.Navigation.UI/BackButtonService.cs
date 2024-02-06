using Windows.UI.Core;

namespace Uno.Extensions.Navigation;

internal class BackButtonService : IHostedService
{
	private readonly ILogger _logger;
	private readonly IRouteNotifier _notifier;
	private readonly NavigationConfiguration? _config;
	private Action? _unregister;
	private IRegion? _rootRegion;

	public BackButtonService(
		ILogger<BackButtonService> logger,
		IRouteNotifier notifier,
		NavigationConfiguration? config)
	{
		_logger = logger;
		_notifier = notifier;
		_config = config;
	}


	public Task StartAsync(CancellationToken cancellationToken)
	{
		if (_logger.IsEnabled(LogLevel.Trace))
		{
			_logger.LogTraceMessage($"Starting {nameof(BackButtonService)}");
		}

		if (_config?.UseNativeBackButton ?? true)
		{
			_notifier.RouteChanged += RouteChanged;

			if (PlatformHelper.IsWebAssembly)
			{
				var currentView = SystemNavigationManager.GetForCurrentView();
				if (currentView != null)
				{
					currentView.BackRequested += NavigationBackRequested;
				}
			}

			_unregister = () =>
			{
				_notifier.RouteChanged -= RouteChanged;
				if (PlatformHelper.IsWebAssembly)
				{
					var currentView = SystemNavigationManager.GetForCurrentView();
					if (currentView != null)
					{
						currentView.BackRequested -= NavigationBackRequested;
					}
				}
			};
		}
		else
		{
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebugMessage($"{nameof(NavigationConfiguration.UseNativeBackButton)} set to false");
			}
		}

		return Task.CompletedTask;
	}

	private async void NavigationBackRequested(object? sender, BackRequestedEventArgs e)
	{
		if (_rootRegion?.Navigator() is { } navigator)
		{
			e.Handled = true;
			await navigator.GoBack(sender ?? this);
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		if (_logger.IsEnabled(LogLevel.Trace))
		{
			_logger.LogTraceMessage($"Starting {nameof(BackButtonService)}");
		}

		var stopAction = _unregister;
		_unregister = default;
		stopAction?.Invoke();

		return Task.CompletedTask;
	}


	private async void RouteChanged(object? sender, RouteChangedEventArgs e)
	{
		try
		{
			_rootRegion = e.Region.Root();
			if (_rootRegion is null)
			{
				return;
			}

			var canGoBack = _rootRegion.Navigator() is { } navigator && await navigator.CanGoBack();

			if (_logger.IsEnabled(LogLevel.Trace))
			{
				_logger.LogTraceMessage($"Can navigate back = {canGoBack}");
			}

			if (PlatformHelper.IsWebAssembly)
			{
				var currentView = SystemNavigationManager.GetForCurrentView();
				if (currentView != null)
				{
					currentView.AppViewBackButtonVisibility = canGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
				}
			}
		}
		catch (Exception ex)
		{
			if (_logger.IsEnabled(LogLevel.Warning))
			{
				_logger.LogWarningMessage($"Error encountered changing back button visibility - {ex.Message}");
			}
		}
	}
}
