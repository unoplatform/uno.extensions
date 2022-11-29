//+:cnd:noEmit
#if markup
using MyExtensionsApp.Extensions;
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
#if markup
			.ConfigureResources()
#endif
//-:cnd:noEmit
			.Configure(host => host
#if DEBUG
				// Switch to Development environment when running in DEBUG
				.UseEnvironment(Environments.Development)
#endif
//+:cnd:noEmit
#if uselogging
				.UseLogging(configure: (context, logBuilder) =>
				{
					// Configure log levels for different categories of logging
					logBuilder.SetMinimumLevel(
						context.HostingEnvironment.IsDevelopment() ?
							LogLevel.Information :
							LogLevel.Warning);
				}, enableUnoLogging: true)
#if useserilog
				.UseSerilog(consoleLoggingEnabled: true, fileLoggingEnabled: true)
#endif
#endif
#if configuration
				.UseConfiguration(configure: configBuilder =>
					configBuilder
						.EmbeddedSource<App>()
						.Section<AppConfig>()
				)
#endif
#if localization
				// Enable localization (see appsettings.json for supported languages)
				.UseLocalization()
#endif
				// Register Json serializers (ISerializer and ISerializer)
				.UseSerialization()
				.ConfigureServices(services => {
					// TODO: Register your services
					//services.AddSingleton<IMyService, MyService>();
				})
#if(reactive)
				.UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes)
#else
				.UseNavigation(RegisterRoutes)
#endif
//-:cnd:noEmit
			)
			// Add navigation support for toolkit controls such as TabBar and NavigationView
			.UseToolkitNavigation();
		_window = builder.Window;

		_host = await builder.ShowAsync<Shell>();
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
				})
		);
	}
}
