namespace Uno.Extensions.Navigation.UI;

internal record NavigationHostedService(ILogger<NavigationRegion> RegionLogger) : IHostedService, IStartupService
{
	private TaskCompletionSource<object> _completion = new TaskCompletionSource<object>();

	public Task StartAsync(CancellationToken cancellationToken)
	{
		Region.Logger = RegionLogger;
		_completion.SetResult(true);
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	public Task StartupComplete() => _completion.Task;
}
