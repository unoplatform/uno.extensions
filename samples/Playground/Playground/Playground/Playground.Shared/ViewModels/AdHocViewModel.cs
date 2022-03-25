using Playground.Services.Endpoints;


using System.Diagnostics;

namespace Playground.ViewModels;

public class AdHocViewModel
{
	private readonly INavigator _navigator;
	private readonly ITodoTaskEndpoint _todoTaskEndpoint;
	private readonly ISerializer<Widget> _serializer;
	public AdHocViewModel(
		INavigator navigator,
		ITodoTaskEndpoint todoTaskEndpoint, 
		ISerializer<Widget> serializer)
	{
		_navigator = navigator;
		_serializer = serializer;
		_todoTaskEndpoint = todoTaskEndpoint;
	}

	public async Task LongRunning()
	{
		await Task.Run(async () =>
		{
			await _navigator.NavigateRouteAsync(this, "./One");
			await Task.Delay(1000);
			await _navigator.NavigateRouteAsync(this, "./Two");
			await Task.Delay(1000);
			await _navigator.NavigateRouteAsync(this, "./Three");
			await Task.Delay(1000);
			await _navigator.NavigateRouteAsync(this, "./One");

		});
	}

	public async Task RunSerializer()
	{
		var w = new Widget { Name = "Bob", Weight = 60 };
	var str = 	_serializer.ToString(w);
		var newW = _serializer.FromString(str);
		Debug.Assert(w == newW);
	}
}
