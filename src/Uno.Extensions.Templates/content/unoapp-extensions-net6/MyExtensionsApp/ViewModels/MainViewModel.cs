//-:cnd:noEmit

using Microsoft.Extensions.Options;
using MyExtensionsApp.Configuration;

namespace MyExtensionsApp.ViewModels;

public class MainViewModel
{
	public string? Title { get; }

	public MainViewModel(
		INavigator navigator,
		IOptions<AppConfig> appInfo)
	{ 
	
		_navigator = navigator;
		Title = appInfo?.Value?.Title;
	}

	public async Task GoToSecondPage()
	{
		await _navigator.NavigateViewModelAsync<SecondViewModel>(this);
	}

	private INavigator _navigator;
}
