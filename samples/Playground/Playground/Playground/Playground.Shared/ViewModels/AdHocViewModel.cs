using Playground.Services.Endpoints;


using System.Diagnostics;
using Uno.Extensions.Storage;

namespace Playground.ViewModels;

public class AdHocViewModel
{
	private readonly INavigator _navigator;
	private readonly IToDoTaskListEndpoint _todoTaskListEndpoint;
	private readonly ISerializer<Widget> _widgetSerializer;
	private readonly ISerializer<Person> _personSerializer;
	private readonly IAuthenticationTokenProvider _authToken;
	private readonly IStorage _dataService;
	private readonly ISerializer _serializer;
	public AdHocViewModel(
		INavigator navigator,
		IAuthenticationTokenProvider authenticationToken,
		IToDoTaskListEndpoint todoTaskEndpoint,
		ISerializer<Widget> widgetSerializer,
		ISerializer<Person> personSerializer,
		IStorage dataService,
		ISerializer serializer)
	{
		_navigator = navigator;
		_authToken = authenticationToken;
		_widgetSerializer = widgetSerializer;
		_personSerializer = personSerializer;
		_todoTaskListEndpoint = todoTaskEndpoint;
		_dataService = dataService;
		_serializer = serializer;
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
		var str = _widgetSerializer.ToString(w);
		var newW = _widgetSerializer.FromString(str);
		Debug.Assert(w == newW);

		var p = new Person { Name = "Jane",Age=25, Height=160.3, Weight = 60 };
		str = _personSerializer.ToString(p);
		var newP = _personSerializer.FromString<Person>(str);
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

	public async Task LoadWidgets()
	{
		var widgets = await _dataService.ReadFileAsync<Widget[]>(_serializer, "data.json");
	}
}
