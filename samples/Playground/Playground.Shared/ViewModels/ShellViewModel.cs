namespace Playground.ViewModels;

public class ShellViewModel
{
	public ShellViewModel(INavigator navigator, ISplashScreen splashScreen )
	{
		_ = Start(navigator, splashScreen);
	}

	private async Task Start(INavigator navigator, ISplashScreen splashScreen)
	{
		var deferral = splashScreen.GetDeferral();

		// Option 1: Use this if not using ShellView (see viewmap in App.xaml.host.cs)
		await navigator.NavigateViewAsync<HomePage>(this);

		// Option 2: Use this if using ShellView, since need to direct the navigation to the frameview (see viewmap in App.xaml.host.cs)
		//await navigator.NavigateViewAsync<HomePage>(this, Qualifiers.Nested);

		deferral.Complete();
	}
}
