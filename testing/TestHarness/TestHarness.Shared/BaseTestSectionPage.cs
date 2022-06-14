

namespace TestHarness;

public partial class BaseTestSectionPage : Page
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
			InitializeHost();
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
				InitializeHost();
			}
		}
	}

	private bool init;
	private void InitializeHost()
	{
		if (init)
		{
			return;
		}
		init = true;
		Host = HostInit!.InitializeHost();

		var win = (Application.Current as App)?.Window!;
		this.AttachServiceProvider(Host.Services).RegisterWindow(win!);

		if (this.FindName(Constants.NavigationRoot) is FrameworkElement root)
		{
			if (root.IsLoaded)
			{

				Region.SetAttached(root, true);
			}
			else
			{

				root.Loaded += (_, _) =>
				{

					Region.SetAttached(root, true);
				};
			}
		}
	}
}

