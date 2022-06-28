//-:cnd:noEmit

namespace MyExtensionsApp.Presentation;

public partial class MainViewModel
{
	public string? Title { get; }

	public MainViewModel(
		INavigator navigator,
		IOptions<AppConfig> appInfo)
	{ 
	
		_navigator = navigator;
		Title = $"Main - {appInfo?.Value?.Title}";
	}

	public async Task GoToSecond(CancellationToken cancellation)
	{
		await _navigator.NavigateViewModelAsync<SecondViewModel>(this, cancellation: cancellation);
	}

	private INavigator _navigator;
}
