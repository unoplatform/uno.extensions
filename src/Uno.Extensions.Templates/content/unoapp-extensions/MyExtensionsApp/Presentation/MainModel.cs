//+:cnd:noEmit
#if(reactive)
using Uno.Extensions.Reactive;
#else
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
#endif

namespace MyExtensionsApp.Presentation;

#if (reactive)
public partial class MainModel 
{
	public string? Title { get; }

	public IState<string> Name { get; }

	public MainModel(
		INavigator navigator,
		IOptions<AppConfig> appInfo)
	{

		_navigator = navigator;
		Title = $"Main - {appInfo?.Value?.Title}";

		Name = State<string>.Value(this, ()=>"");
	}

	public async Task GoToSecond()
	{
		var name = await Name;
		await _navigator.NavigateViewModelAsync<SecondModel>(this, data: new Entity(name!));
	}

	private INavigator _navigator;
}
#else
public partial class MainModel:ObservableObject
{
	public string? Title { get; }

	[ObservableProperty]
	private string? name;

	public ICommand GoToSecond { get; }

	public MainModel(
		INavigator navigator,
		IOptions<AppConfig> appInfo)
	{ 
	
		_navigator = navigator;
		Title = $"Main - {appInfo?.Value?.Title}";

		GoToSecond = new AsyncRelayCommand(GoToSecondView);
	}

	public async Task GoToSecondView()
	{
		await _navigator.NavigateViewModelAsync<SecondModel>(this, data: new Entity(Name!));
	}

	private INavigator _navigator;
}
#endif

//-:cnd:noEmit

