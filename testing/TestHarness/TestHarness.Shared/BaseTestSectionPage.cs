

namespace TestHarness;

public partial class BaseTestSectionPage : Page, IDisposable
{
	protected IHost? Host { get; set; }

	public BaseTestSectionPage()
	{
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
			return (root!.Content as FrameworkElement)!.Navigator()!;
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
		Host = await win.InitializeNavigationWithExtendedSplash(HostInit!.InitializeHost, navigationRoot: navigationRoot);
	}

	public void Dispose()
	{
		_ = Host?.StopAsync();
	}
}

