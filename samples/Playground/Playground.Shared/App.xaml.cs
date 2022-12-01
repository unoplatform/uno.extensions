namespace Playground;

public sealed partial class App : Application
{
	private Window? _window;
	public Window? Window => _window;

	public App()
	{
		this.InitializeComponent();
	}

	/// <summary>
	/// Invoked when the application is launched normally by the end user.  Other entry points
	/// will be used such as when the application is launched to open a specific file.
	/// </summary>
	/// <param name="args">Details about the launch request and process.</param>
	protected async override void OnLaunched(LaunchActivatedEventArgs args)
	{
#if DEBUG
		if (System.Diagnostics.Debugger.IsAttached)
		{
			// this.DebugSettings.EnableFrameRateCounter = true;
		}
#endif

		var appBuilder = this.CreateBuilder(args)
			.ConfigureApp()
			.UseToolkitNavigation();
		_window = appBuilder.Window;
#if NET5_0_OR_GREATER && WINDOWS
		_window.Activate();
#endif

		var hostingOption = InitOption.Splash;

		switch (hostingOption)
		{
			case InitOption.AdHocHosting:
				// Ad-hoc hosting of Navigation on a UI element with Region.Attached set


				_host = appBuilder.Build();

				// Create Frame and navigate to MainPage
				// MainPage has a ContentControl with Region.Attached set
				// which will host navigation
				var f = new Frame();
				_window.Content = f;
				_window.AttachServices(_host.Services);
				f.Navigate(typeof(MainPage));

				await Task.Run(() => _host.StartAsync());

				// With this way there's no way to await for navigation to finish
				// but it's useful if you want to attach navigation to a UI element
				// in an existing application
				break;

			case InitOption.NavigationRoot:
				// Explicitly create the navigation root to use

				_host = appBuilder.Build();

				var root = new ContentControl
				{
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
					HorizontalContentAlignment = HorizontalAlignment.Stretch,
					VerticalContentAlignment = VerticalAlignment.Stretch
				};
				_window.Content = root;
				var services = _window.AttachServices(_host.Services);
				var startup = root.HostAsync(services, initialRoute: "");

				await Task.Run(() => _host.StartAsync());

				// Wait for startup task to complete which will be the end of the
				// first navigation
				await startup;
				break;

			case InitOption.AttachNavigation:
#pragma warning disable CS0618 // AttachNavigation is obsolete - init option is left here to test backward compat
				// Attach navigation to a Window which will create the navigation root
				// and assign to Content property on the Window
				_host = appBuilder.Build();

				_window.AttachNavigation(_host.Services,
					// Option 1: This requires Shell to be the first RouteMap - best for perf as no reflection required
					// initialRoute: ""
					// Option 2: Specify route name
					// initialRoute: "Shell"
					// Option 3: Specify the view model. To avoid reflection, you can still define a routemap
					initialViewModel: typeof(ShellViewModel)
					);

				await Task.Run(() => _host.StartAsync());

				// With this way there's no way to await for navigation to finish
				break;
#pragma warning restore CS0618 // Type or member is obsolete

			case InitOption.InitializeNavigation:
				// InitializeNavigationAsync will create the navigation host (ContentControl),
				// will invoke the host builder (host is returned) and awaits both start up
				// tasks, as well as first navigation

				_host = await _window.InitializeNavigationAsync(async () => appBuilder.Build(),
							// Option 1: This requires Shell to be the first RouteMap - best for perf as no reflection required
							// initialRoute: ""
							// Option 2: Specify route name
							// initialRoute: "Shell"
							// Option 3: Specify the view model. To avoid reflection, you can still define a routemap
							initialViewModel: typeof(ShellViewModel)
						);
				break;

			case InitOption.Splash:
				// InitializeNavigationAsync (Navigation.Toolkit) uses a LoadingView as navigation host,
				// will invoke the host builder (host is returned) and awaits both start up
				// tasks, as well as first navigation. In this case the navigation host is an ExtendedSplashScreen
				// element, so will show the native splash screen until the first navigation is completed

				var appRoot = new AppRoot();
				appRoot.SplashScreen.Initialize(_window, args);

				_window.Content = appRoot;
				_window.Activate();

				_host = await _window.InitializeNavigationAsync(
							async () =>
							{

								// Uncomment to view splashscreen for longer
								// await Task.Delay(5000);
								return appBuilder.Build();
							},
							navigationRoot: appRoot.SplashScreen,
							// Option 1: This requires Shell to be the first RouteMap - best for perf as no reflection required
							// initialRoute: ""
							// Option 2: Specify route name
							// initialRoute: "Shell"
							// Option 3: Specify the view model. To avoid reflection, you can still define a routemap
							initialViewModel: typeof(ShellViewModel)
						);
				break;

			case InitOption.AppBuilderShell:

				_host = await appBuilder.ShowAsync<AppRoot>();
				break;

			case InitOption.NoShellViewModel:
				// InitializeNavigationAsync with splash screen and async callback to determine where
				// initial navigation should go

				var appRootNoShell = new AppRoot();
				appRootNoShell.SplashScreen.Initialize(_window, args);

				_window.Content = appRootNoShell;
				_window.Activate();

				_host = await _window.InitializeNavigationAsync(
							async () =>
							{
								return appBuilder.Build();
							},
							navigationRoot: appRootNoShell.SplashScreen,
							initialNavigate: async (sp, nav)=>
							{
								// Uncomment to view splashscreen for longer
								await Task.Delay(5000);
								var ts = sp.GetRequiredService<IThemeService>();
								await ts.SetThemeAsync(ts.Theme);
								await nav.NavigateViewAsync<HomePage>(this);
							}
						);

				break;
		}

		var notif = _host!.Services.GetRequiredService<IRouteNotifier>();
		notif.RouteChanged += RouteUpdated;


		var logger = _host.Services.GetRequiredService<ILogger<App>>();
		if (logger.IsEnabled(LogLevel.Trace)) logger.LogTraceMessage("LogLevel:Trace");
		if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebugMessage("LogLevel:Debug");
		if (logger.IsEnabled(LogLevel.Information)) logger.LogInformationMessage("LogLevel:Information");
		if (logger.IsEnabled(LogLevel.Warning)) logger.LogWarningMessage("LogLevel:Warning");
		if (logger.IsEnabled(LogLevel.Error)) logger.LogErrorMessage("LogLevel:Error");
		if (logger.IsEnabled(LogLevel.Critical)) logger.LogCriticalMessage("LogLevel:Critical");
	}

	private enum InitOption
	{
		AdHocHosting,
		NavigationRoot,
		AttachNavigation,
		InitializeNavigation,
		Splash,
		NoShellViewModel,
		AppBuilderShell
	}


	public void RouteUpdated(object? sender, RouteChangedEventArgs? e)
	{
		try
		{
			var rootRegion = e?.Region.Root();
			var route = rootRegion?.GetRoute();
			if (route is null)
			{
				return;
			}


#if !__WASM__ && !WINUI
			CoreApplication.MainView?.DispatcherQueue.TryEnqueue(() =>
			{
				var appTitle = ApplicationView.GetForCurrentView();
				appTitle.Title = "Commerce: " + (route + "").Replace("+", "/");
			});
#endif

		}
		catch (Exception ex)
		{
			Console.WriteLine("Error: " + ex.Message);
		}
	}

}
