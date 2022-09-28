//-:cnd:noEmit

namespace MyExtensionsApp.Presentation;

public class ShellViewModel
{
	private INavigator Navigator { get; }


	public ShellViewModel(
		INavigator navigator,
		ISplashScreen splash)
	{

		Navigator = navigator;

		_ = Start(splash);
	}

	public async Task Start(ISplashScreen splash)
	{
		var deferral = splash.GetDeferral();
		try
		{
			await Task.Delay(5000);
			await Navigator.NavigateViewModelAsync<MainViewModel>(this, Qualifiers.Nested);
		}
		finally
		{
			deferral.Complete();
		}
	}
}
