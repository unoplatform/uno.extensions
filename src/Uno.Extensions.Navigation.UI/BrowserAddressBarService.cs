using Windows.UI.Core;

namespace Uno.Extensions.Navigation;

internal class BrowserAddressBarService : IHostedService
{
	private readonly ILogger _logger;
	private readonly IRouteNotifier _notifier;
	private readonly IHasAddressBar? _addressbarHost;
	private readonly NavigationConfiguration? _config;
	private Action? _unregister;

	public BrowserAddressBarService(
		ILogger<BrowserAddressBarService> logger,
		IRouteNotifier notifier,
		NavigationConfiguration? config,
		IHasAddressBar? host = null)
	{
		_logger = logger;
		_notifier = notifier;
		_addressbarHost = host;
		_config = config;
	}


	public Task StartAsync(CancellationToken cancellationToken)
	{
		if (_logger.IsEnabled(LogLevel.Trace))
		{
			_logger.LogTraceMessage($"Starting {nameof(BrowserAddressBarService)}");
		}

		if (_addressbarHost is not null && (_config?.AddressBarUpdateEnabled ?? true))
		{
			_notifier.RouteChanged += RouteChanged;
			_unregister = () => _notifier.RouteChanged -= RouteChanged;
		}
		else
		{
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebugMessage($"{nameof(IHasAddressBar)} not defined, or {nameof(NavigationConfiguration.AddressBarUpdateEnabled)} set to false");
			}
		}

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		if (_logger.IsEnabled(LogLevel.Trace))
		{
			_logger.LogTraceMessage($"Stopping {nameof(BrowserAddressBarService)}");
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
			var rootRegion = e.Region.Root();
			var route = rootRegion.GetRoute();
			if (route is null)
			{
				return;
			}

			var canGoBack = rootRegion.Navigator() is { } navigator && await navigator.CanGoBack();

			var url = new UriBuilder
			{
				Query = route.Query(),
				Path = route.FullPath()?.Replace("+", "/")
			};
			await _addressbarHost!.UpdateAddressBar(url.Uri, canGoBack);
		}
		catch (Exception ex)
		{
			if (_logger.IsEnabled(LogLevel.Warning))
			{
				_logger.LogWarningMessage($"Error encountered updating address bar on route changed event - {ex.Message}");
			}
		}
	}
}
