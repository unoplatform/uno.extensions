using Uno.Extensions.Diagnostics;

namespace TestHarness;
public partial class App : Application
{
	private Window? _window;
	public Window Window => _window!;

	public App()
	{
		PerformanceTimer.InitializeTimers();
		this.InitializeComponent();
	}

	protected override async void OnLaunched(LaunchActivatedEventArgs args)
	{
#if WINDOWS10_0_19041_0_OR_GREATER
		// This is only required because we don't run UnoHost.CreateDefaultHost until a
		// test scenario is selected. This line is included to ensure web auth test cases
		// work, without having to navigate to a test scenario in the new app instance
		// that is launched to handle the web auth redirect
		WinUIEx.WebAuthenticator.CheckOAuthRedirectionActivation();
#endif


#if NET6_0_OR_GREATER && WINDOWS10_0_19041_0_OR_GREATER
				_window = new Window();
#else
		_window = Window.Current;
#endif

		// Need to manually create and then dispose an IHost in order to set
		// the correct locale for the app. This is required for the Localization
		// tests to work when app is restarted
		var host = UnoHost
					.CreateDefaultBuilder()
					.UseConfiguration(
						configureHostConfiguration: builder => builder.AddSectionFromEntity(new LocalizationConfiguration { Cultures = new[] { "es", "en", "en-AU", "fr" } }))
					.UseLocalization()
					.Build();
		var locals = host.Services.GetServices<IServiceInitialize>();
		foreach (var local in locals.OfType<IDisposable>())
		{
			local.Dispose();
		}

		var rootFrame = _window.Content as TestFrameHost;

#if __IOS__ && USE_UITESTS && !__MACCATALYST__
		// requires Xamarin Test Cloud Agent
		Xamarin.Calabash.Start();
#endif

		// Do not repeat app initialization when the Window already has content,
		// just ensure that the window is active
		if (rootFrame == null)
		{
			// Create a Frame to act as the navigation context and navigate to the first page
			rootFrame = new TestFrameHost();


			// Place the frame in the current Window
			_window.Content = rootFrame;

			//_window.GetThemeService();

		}
		_window.Activate();
	}
}
