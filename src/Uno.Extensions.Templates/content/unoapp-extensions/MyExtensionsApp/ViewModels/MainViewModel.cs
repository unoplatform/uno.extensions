//-:cnd:noEmit

using Microsoft.Extensions.Options;

namespace MyExtensionsApp.ViewModels;

public class MainViewModel
{
	public string? Title { get; }

	public MainViewModel(
		INavigator navigator,
		IOptions<AppInfo> appInfo)
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
