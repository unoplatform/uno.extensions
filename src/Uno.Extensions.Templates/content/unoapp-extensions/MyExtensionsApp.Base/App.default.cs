//+:cnd:noEmit
#if useCsharpMarkup
using MyExtensionsApp.Infrastructure;
#endif
using Microsoft.UI.Xaml;
using Application = Microsoft.UI.Xaml.Application;

namespace MyExtensionsApp;

public sealed partial class App : Application
{
	public App()
	{
		this.InitializeComponent();
	}

	/// <summary>
	/// Invoked when the application is launched normally by the end user.  Other entry points
	/// will be used such as when the application is launched to open a specific file.
	/// </summary>
	/// <param name="args">Details about the launch request and process.</param>
#if useFrameNav
	protected override void OnLaunched(LaunchActivatedEventArgs args) =>
		AppStart.OnLaunched(this, args);
#else
	protected async override void OnLaunched(LaunchActivatedEventArgs args) =>
		await AppStart.OnLaunched(this, args);
#endif
}
