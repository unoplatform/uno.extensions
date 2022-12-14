//+:cnd:noEmit
#if use-csharp-markup
using MyExtensionsApp.Infrastructure;
#endif
//-:cnd:noEmit

namespace MyExtensionsApp;

public sealed partial class App : Application
{
	private Window? _window;
	private IHost? _host;

	public App()
	{
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
		var builder = this.CreateBuilder(args)
//+:cnd:noEmit
#if use-csharp-markup
			.ConfigureResources()
#endif
#if use-toolkit-nav
			// Add navigation support for toolkit controls such as TabBar and NavigationView
			.UseToolkitNavigation()
#endif
//-:cnd:noEmit
			.Configure(host => host
#if DEBUG
				// Switch to Development environment when running in DEBUG
				.UseEnvironment(Environments.Development)
#endif
//+:cnd:noEmit
#if use-logging
				.UseLogging(configure: (context, logBuilder) =>
				{
					// Configure log levels for different categories of logging
					logBuilder.SetMinimumLevel(
						context.HostingEnvironment.IsDevelopment() ?
							LogLevel.Information :
							LogLevel.Warning);
				}, enableUnoLogging: true)
#if use-serilog
				.UseSerilog(consoleLoggingEnabled: true, fileLoggingEnabled: true)
#endif
#endif
#if use-configuration
				.UseConfiguration(configure: configBuilder =>
					configBuilder
						.EmbeddedSource<App>()
						.Section<AppConfig>()
				)
#endif
#if use-localization
				// Enable localization (see appsettings.json for supported languages)
				.UseLocalization()
#endif
				// Register Json serializers (ISerializer and ISerializer)
				.UseSerialization()
				.ConfigureServices((context, services) => {
#if use-http
					// Register HttpClient
					services
						.AddTransient<DebugHttpHandler>()
						.AddRefitClient<IApiClient>(context
//-:cnd:noEmit
#if DEBUG
							, configure: (builder, endpoint) =>
								builder.ConfigurePrimaryAndInnerHttpMessageHandler<DebugHttpHandler>()
#endif
//+:cnd:noEmit
						);

#endif
					// TODO: Register your services
					//services.AddSingleton<IMyService, MyService>();
				})
#if (use-toolkit-nav && reactive)
				.UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes)
#elif (use-toolkit-nav)
				.UseNavigation(RegisterRoutes)
#endif
			);
		_window = builder.Window;

#if use-toolkit-nav
		_host = await builder.NavigateAsync<Shell>();
#elif use-frame-nav
		// Do not repeat app initialization when the Window already has content,
		// just ensure that the window is active
		if (_window.Content is not Frame rootFrame)
		{
			// Create a Frame to act as the navigation context and navigate to the first page
			rootFrame = new Frame();

			rootFrame.NavigationFailed += OnNavigationFailed;

			if (args.UWPLaunchActivatedEventArgs.PreviousExecutionState == ApplicationExecutionState.Terminated)
			{
				// TODO: Load state from previously suspended application
			}

			// Place the frame in the current Window
			_window.Content = rootFrame;
		}

//-:cnd:noEmit
#if !(NET6_0_OR_GREATER && WINDOWS)
		if (args.UWPLaunchActivatedEventArgs.PrelaunchActivated == false)
#endif
//+:cnd:noEmit
		{
			if (rootFrame.Content == null)
			{
				// When the navigation stack isn't restored navigate to the first page,
				// configuring the new page by passing required information as a navigation
				// parameter
				rootFrame.Navigate(typeof(MainPage), args.Arguments);
			}
			// Ensure the current window is active
			_window.Activate();
		}
		_host = builder.Build();
		await _host.StartAsync();
#endif
	}
#if use-frame-nav

	/// <summary>
	/// Invoked when Navigation to a certain page fails
	/// </summary>
	/// <param name="sender">The Frame which failed navigation</param>
	/// <param name="e">Details about the navigation failure</param>
	void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
	{
		throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
	}
#endif

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
#if use-toolkit-nav

	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap(ViewModel: typeof(ShellModel)),
			new ViewMap<MainPage, MainModel>(),
			new DataViewMap<SecondPage, SecondModel, Entity>()
		);

		routes.Register(
			new RouteMap("", View: views.FindByViewModel<ShellModel>(),
				Nested: new RouteMap[]
				{
					new RouteMap("Main", View: views.FindByViewModel<MainModel>()),
					new RouteMap("Second", View: views.FindByViewModel<SecondModel>()),
				}
			)
		);
	}
#endif
}
