namespace Uno.Extensions.Navigation.UI;

internal record NavigationHostedService(ILogger<NavigationRegion> RegionLogger) : IHostedService
{
	public Task StartAsync(CancellationToken cancellationToken)
	{
		Region.Logger = RegionLogger;
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
