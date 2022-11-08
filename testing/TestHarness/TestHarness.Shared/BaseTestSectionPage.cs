

using Uno.Extensions.Diagnostics;
using Uno.Toolkit.UI;

namespace TestHarness;

public partial class BaseTestSectionPage : Page, IDisposable
{
	protected IHost? Host { get; set; }

	public BaseTestSectionPage()
	{
		PerformanceTimer.InitializeTimers();
		Loaded += BaseTestSectionPage_Loaded;
	}

	private IHostInitialization? HostInit { get; set; }
	private void BaseTestSectionPage_Loaded(object sender, RoutedEventArgs e)
	{
		if (HostInit is not null)
		{
			_ = InitializeHost();
		}
	}
	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		if (e.Parameter is IHostInitialization hostInit)
		{
			HostInit = hostInit;

			if (this.IsLoaded)
			{
				_ = InitializeHost();
			}
		}
	}

	protected INavigator Navigator
	{
		get
		{
			var root = this.FindName(Constants.NavigationRoot) as ContentControl;
			var nav = root?.Navigator();
			return nav!;
		}
	}

	private bool init;
	private async Task InitializeHost()
	{
		if (init)
		{
			return;
		}
		init = true;

		var win = (Application.Current as App)?.Window!;
		var navigationRoot = this.FindName(Constants.NavigationRoot) as ContentControl;
		if(navigationRoot is LoadingView loadingView)
		{
			if (loadingView is ExtendedSplashScreen splash)
			{
				splash.Window = win;
			}
			Host = await win.InitializeNavigationAsync(async ()=>
			{
				// Uncomment this delay to see the loading/splash view for longer
				// The Navigation/Apps/Commerce example uses an ExtendedSplashScreen in CommerceMainPage
				// await Task.Delay(5000);
				return HostInit!.InitializeHost();
			}, navigationRoot: loadingView);
		}
		else
		{
			Host = await win.InitializeNavigationAsync(async ()=>HostInit!.InitializeHost(), navigationRoot: navigationRoot);
		}
	}

	public void Dispose()
	{
		_ = Host?.StopAsync();
	}
}

