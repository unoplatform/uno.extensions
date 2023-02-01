using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyExtensionsApp.Presentation;

public partial class MainViewModel : ObservableObject
{
	public string? Title { get; }

	[ObservableProperty]
	private string? name;

	public ICommand GoToSecond { get; }

//+:cnd:noEmit
#if useConfiguration
	public MainViewModel(
		INavigator navigator,
		IOptions<AppConfig> appInfo)
	{
		_navigator = navigator;
		Title = $"Main - {appInfo?.Value?.Title}";
#else
	public MainViewModel(INavigator navigator)
	{
		_navigator = navigator;
		Title = "Main - MyExtensionsApp";
#endif
//-:cnd:noEmit
		GoToSecond = new AsyncRelayCommand(GoToSecondView);
	}

	private async Task GoToSecondView()
	{
		await _navigator.NavigateViewModelAsync<SecondViewModel>(this, data: new Entity(Name!));
	}

	private INavigator _navigator;
}
