using Playground.Services.Endpoints;

namespace Playground.ViewModels;

public class AdHocViewModel
{
	private readonly INavigator _navigator;
	private readonly ITodoTaskEndpoint _todoTaskEndpoint;
	public AdHocViewModel(
		INavigator navigator,
		ITodoTaskEndpoint todoTaskEndpoint)
	{
		_navigator = navigator;
		_todoTaskEndpoint = todoTaskEndpoint;
	}

	public async Task LongRunning()
	{
		await Task.Run(async () =>
		{
			await _navigator.NavigateRouteAsync(this,"./One");
			await Task.Delay(1000);
			await _navigator.NavigateRouteAsync(this, "./Two");
			await Task.Delay(1000);
			await _navigator.NavigateRouteAsync(this, "./Three");
			await Task.Delay(1000);
			await _navigator.NavigateRouteAsync(this, "./One");

		});
	}
}
