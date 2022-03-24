using Uno.Extensions.Hosting;

namespace Uno.Extensions.Navigation;

internal class BrowserAddressBarService : IHostedService
{
	private readonly IRouteNotifier _notifier;
	private readonly IHasAddressBar? _addressbarHost;
	private readonly NavigationConfig? _config;
	public BrowserAddressBarService(
		IRouteNotifier notifier,
		NavigationConfig? config,
		IHasAddressBar? host = null)
	{
		_notifier = notifier;
		_addressbarHost = host;
		_config = config;
	}


	public Task StartAsync(CancellationToken cancellationToken)
	{
		if (_addressbarHost is not null && (_config?.AddressBarUpdateEnabled??true))
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


	private async void RouteChanged(object sender, RouteChangedEventArgs e)
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
			Console.WriteLine("Error: " + ex.Message);
		}
	}
}
