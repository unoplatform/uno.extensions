

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
		Console.WriteLine("*********** Loaded");
		if (HostInit is not null)
		{
			Console.WriteLine("*********** HostInit is not null");

			InitializeHost();
		}
	}
	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		Console.WriteLine("*********** OnNavigatedTo");
		if (e.Parameter is IHostInitialization hostInit)
		{
			HostInit = hostInit;

			if (this.IsLoaded)
			{
				Console.WriteLine("*********** this. IsLoaded");

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
		Console.WriteLine("*********** hostInit not null");
		Host = HostInit!.InitializeHost();
		Console.WriteLine("*********** Host");

		var win = (Application.Current as App)?.Window!;
		Console.WriteLine($"*********** Win exists {win is not null}");
		this.AttachServiceProvider(Host.Services).RegisterWindow(win!);
		Console.WriteLine("*********** Attached and Registered");

		if (this.FindName(Constants.NavigationRoot) is FrameworkElement root)
		{
			Console.WriteLine("*********** NavigationRoot found");
			if (root.IsLoaded)
			{
				Console.WriteLine("*********** IsLoaded");

				Region.SetAttached(root, true);
			}
			else
			{
				Console.WriteLine("*********** Not Loaded");

				root.Loaded += (_, _) =>
				{
					Console.WriteLine("*********** Now Loaded");

					Region.SetAttached(root, true);
				};
			}
		}
	}
}

