using Playground.Services.Endpoints;


using System.Diagnostics;

namespace Playground.ViewModels;

public class AdHocViewModel
{
	private readonly INavigator _navigator;
	private readonly IToDoTaskListEndpoint _todoTaskListEndpoint;
	private readonly ISerializer<Widget> _serializer;
	private readonly IAuthenticationTokenProvider _authToken;
	public AdHocViewModel(
		INavigator navigator,
		IAuthenticationTokenProvider authenticationToken,
		IToDoTaskListEndpoint todoTaskListEndpoint, 
		ISerializer<Widget> serializer)
	{
		_navigator = navigator;
		_authToken = authenticationToken;
		_serializer = serializer;
		_todoTaskListEndpoint = todoTaskListEndpoint;
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

	public async Task FetchTasks()
	{
		var response = await _navigator.NavigateRouteForResultAsync<string>(this, "Auth", qualifier: Qualifiers.Dialog);
		if(response?.Result is null)
		{
			return;
		}

		var result = await response.Result;
		(_authToken as SimpleAuthenticationToken).AccessToken = result.SomeOrDefault() ?? String.Empty;
		var taskLists = await _todoTaskListEndpoint.GetAllAsync(CancellationToken.None);
	}
}
