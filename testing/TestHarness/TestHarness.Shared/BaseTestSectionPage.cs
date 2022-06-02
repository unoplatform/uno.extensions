

namespace TestHarness;

public class BaseTestSectionPage:Page
{
	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);

		if (e.Parameter is IHostInitialization hostInit)
		{
			var host = hostInit.InitializeHost();
			this.AttachServiceProvider(host.Services).RegisterWindow((Application.Current as App)!.Window);

			if (this.FindName(Constants.NavigationRoot) is DependencyObject root)
			{

				Region.SetAttached(root, true);
			}
		}
	}
}
