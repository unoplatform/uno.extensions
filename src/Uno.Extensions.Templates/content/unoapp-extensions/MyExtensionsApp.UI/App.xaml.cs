//-:cnd:noEmit

namespace MyExtensionsApp;

public sealed partial class App : Application
{
	private Window? _window;

	private IHost Host { get; }

	public App()
	{
		Host = UnoHost
				.CreateDefaultBuilder(true)
#if DEBUG
				// Switch to Development environment when running in DEBUG
				.UseEnvironment(Environments.Development)
#endif


				// Add platform specific log providers
				.UseLogging()

				// Configure log levels for different categories of logging
				.ConfigureLogging(logBuilder =>
				{
					logBuilder
							.SetMinimumLevel(LogLevel.Information)
							.XamlLogLevel(LogLevel.Information)
							.XamlLayoutLogLevel(LogLevel.Information);
				})

				// Load configuration information from appsettings.json
				.UseAppSettings()

				// Load AppInfo section
				.UseConfiguration<AppInfo>()

				// Register Json serializers (ISerializer and IStreamSerializer)
				.UseSerialization()

				// Register services for the application
				.ConfigureServices(services =>
				{
					//services

					//	.AddSingleton<IProductService, ProductService>()
					//	.AddSingleton<ICartService, CartService>()
					//	.AddSingleton<IDealService, DealService>()
					//	.AddSingleton<IProfileService, ProfileService>();
				})


				// Enable navigation, including registering views and viewmodels
				.UseNavigation(RegisterRoutes)

				// Add navigation support for toolkit controls such as TabBar and NavigationView
				.UseToolkitNavigation()


				.Build(enableUnoLogging: true);

		this.InitializeComponent();

#if HAS_UNO || NETFX_CORE
		this.Suspending += OnSuspending;
#endif
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

#if NET5_0 && WINDOWS
		_window = new Window();
		_window.Activate();
#else
#if WINUI
		_window = Microsoft.UI.Xaml.Window.Current;
#else
		_window = Windows.UI.Xaml.Window.Current;
#endif
#endif

		var notif = Host.Services.GetService<IRouteNotifier>();
		if (notif is not null)
		{
			notif.RouteChanged += RouteUpdated;
		}


		_window.Content = Host.Services.NavigationHost();
		_window.Activate();

		await Task.Run(async () =>
		{
			await Host.StartAsync();
		});

	}

	/// <summary>
	/// Invoked when Navigation to a certain page fails
	/// </summary>
	/// <param name="sender">The Frame which failed navigation</param>
	/// <param name="e">Details about the navigation failure</param>
	void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
	{
		throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
	}

	/// <summary>
	/// Invoked when application execution is being suspended.  Application state is saved
	/// without knowing whether the application will be terminated or resumed with the contents
	/// of memory still intact.
	/// </summary>
	/// <param name="sender">The source of the suspend request.</param>
	/// <param name="e">Details about the suspend request.</param>
	private void OnSuspending(object sender, SuspendingEventArgs e)
	{
		var deferral = e.SuspendingOperation.GetDeferral();
		// TODO: Save application state and stop any background activity
		deferral.Complete();
	}

	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<ShellControl,ShellViewModel>(),
			new ViewMap<MainPage, MainViewModel>(),
			new ViewMap<SecondPage, SecondViewModel>()
			);

		routes
			.Register(
			views =>
				new("", View: views.FindByViewModel<ShellViewModel>(),
						Nested: new RouteMap[]
						{
										new ("Main",
												View: views.FindByViewModel<MainViewModel>(),
												IsDefault: true
												),
										new("Second",
												View: views.FindByViewModel<SecondViewModel>(),
												DependsOn:"Main"),
						}));
	}

	public async void RouteUpdated(object? sender, RouteChangedEventArgs e)
	{
		try
		{
			var rootRegion = e.Region.Root();
			var route = rootRegion.GetRoute();


#if !__WASM__ && !WINUI
			CoreApplication.MainView?.DispatcherQueue.TryEnqueue(() =>
			{
				var appTitle = ApplicationView.GetForCurrentView();
				appTitle.Title = "MyExtensionsApp: " + (route + "").Replace("+", "/");
			});
#endif


#if __WASM__
			// Note: This is a hack to avoid error being thrown when loading products async
			await Task.Delay(1000).ConfigureAwait(false);
			CoreApplication.MainView?.DispatcherQueue.TryEnqueue(() =>
			{
				var href = WebAssemblyRuntime.InvokeJS("window.location.href");
				var url = new UriBuilder(href);
				url.Query = route?.Query();
				url.Path = route?.FullPath()?.Replace("+", "/");
				var webUri = url.Uri.OriginalString;
				var js = $"window.history.pushState(\"{webUri}\",\"\", \"{webUri}\");";
				Console.WriteLine($"JS:{js}");
				var result = WebAssemblyRuntime.InvokeJS(js);
			});
#endif
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error: " + ex.Message);
		}
	}
}
