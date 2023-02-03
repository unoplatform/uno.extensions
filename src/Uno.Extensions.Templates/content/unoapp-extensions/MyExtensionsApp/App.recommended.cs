namespace MyExtensionsApp;

public class App : Application
{
	private static Window? _window;
	public static IHost? Host { get; private set; }

//+:cnd:noEmit
#if useFrameNav
	protected override void OnLaunched(LaunchActivatedEventArgs args)
#else
	protected async override void OnLaunched(LaunchActivatedEventArgs args)
#endif
	{
		var builder = this.CreateBuilder(args)

#if (useDefaultNav)
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
#if useLogging
				.UseLogging(configure: (context, logBuilder) =>
				{
					// Configure log levels for different categories of logging
					logBuilder.SetMinimumLevel(
						context.HostingEnvironment.IsDevelopment() ?
							LogLevel.Information :
							LogLevel.Warning);
				}, enableUnoLogging: true)
#endif
#if useSerilog
				.UseSerilog(consoleLoggingEnabled: true, fileLoggingEnabled: true)
#endif
#if useConfiguration
				.UseConfiguration(configure: configBuilder =>
					configBuilder
						.EmbeddedSource<App>()
						.Section<AppConfig>()
				)
#endif
#if useLocalization
				// Enable localization (see appsettings.json for supported languages)
				.UseLocalization()
#endif
				// Register Json serializers (ISerializer and ISerializer)
				.UseSerialization()
				.ConfigureServices((context, services) => {
#if useHttp
					// Register HttpClient
					services
//-:cnd:noEmit
#if DEBUG
						// DelegatingHandler will be automatically injected into Refit Client
						.AddTransient<DelegatingHandler, DebugHttpHandler>()
#endif
//+:cnd:noEmit
						.AddRefitClient<IApiClient>(context);

#endif
					// TODO: Register your services
					//services.AddSingleton<IMyService, MyService>();
				})
#if (useReactiveExtensionsNavigation)
				.UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes)
#elif (useExtensionsNavigation)
				.UseNavigation(RegisterRoutes)
#endif
			);
		_window = builder.Window;

#if useFrameNav
//-:cnd:noEmit
		Host = builder.Build();

		// Do not repeat app initialization when the Window already has content,
		// just ensure that the window is active
		if (_window.Content is not Frame rootFrame)
		{
			// Create a Frame to act as the navigation context and navigate to the first page
			rootFrame = new Frame();

			// Place the frame in the current Window
			_window.Content = rootFrame;
		}

		if (rootFrame.Content == null)
		{
			// When the navigation stack isn't restored navigate to the first page,
			// configuring the new page by passing required information as a navigation
			// parameter
			rootFrame.Navigate(typeof(MainPage), args.Arguments);
		}
		// Ensure the current window is active
		_window.Activate();
//+:cnd:noEmit
#else
		Host = await builder.NavigateAsync<Shell>();
#endif
	}

#if !useFrameNav
	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
#if (useDefaultNavMvux)
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
#elif (useDefaultNavMvvm)
		views.Register(
			new ViewMap(ViewModel: typeof(ShellViewModel)),
			new ViewMap<MainPage, MainViewModel>(),
			new DataViewMap<SecondPage, SecondViewModel, Entity>()
		);

		routes.Register(
			new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
				Nested: new RouteMap[]
				{
					new RouteMap("Main", View: views.FindByViewModel<MainViewModel>()),
					new RouteMap("Second", View: views.FindByViewModel<SecondViewModel>()),
				}
			)
		);
#endif
	}
#endif
}
