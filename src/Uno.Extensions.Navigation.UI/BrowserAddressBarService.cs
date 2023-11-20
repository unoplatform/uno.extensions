namespace Uno.Extensions.Navigation;

internal class BrowserAddressBarService : IHostedService
{
	private readonly ILogger _logger;
	private readonly IRouteNotifier _notifier;
	private readonly IHasAddressBar? _addressbarHost;
	private readonly NavigationConfig? _config;
	public BrowserAddressBarService(
		ILogger<BrowserAddressBarService> logger,
		IRouteNotifier notifier,
		NavigationConfig? config,
		IHasAddressBar? host = null)
	{
		_logger = logger;
		_notifier = notifier;
		_addressbarHost = host;
		_config = config;
	}


	public Task StartAsync(CancellationToken cancellationToken)
	{
		if (_addressbarHost is not null && (_config?.AddressBarUpdateEnabled ?? true))
		{
			_notifier.RouteChanged += RouteChanged;
		}

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		if (_addressbarHost is not null)
		{
			_notifier.RouteChanged -= RouteChanged;
		}

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

			var url = new UriBuilder();
			url.Query = route.Query();
			url.Path = route.FullPath()?.Replace("+", "/");
			await _addressbarHost!.UpdateAddressBar(url.Uri);
		}
		catch (Exception ex)
		{
			if (_logger.IsEnabled(LogLevel.Warning))
			{
				_logger.LogWarning($"Error encountered updating address bar on route changed event - {ex.Message}");
			}
		}
	}
}
