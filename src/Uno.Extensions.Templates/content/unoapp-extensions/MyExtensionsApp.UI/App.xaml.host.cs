//-:cnd:noEmit
namespace MyExtensionsApp;

public sealed partial class App : Application
{
	private IHost? Host { get; set; }

	private static IHost BuildAppHost()
	{
		return UnoHost
				.CreateDefaultBuilder()
#if DEBUG
				// Switch to Development environment when running in DEBUG
				.UseEnvironment(Environments.Development)
#endif
				// Add platform specific log providers
				.UseLogging(configure: (context, logBuilder) =>
				{
					// Configure log levels for different categories of logging
					logBuilder
							.SetMinimumLevel(
								context.HostingEnvironment.IsDevelopment() ?
									LogLevel.Information :
									LogLevel.Warning);
				})

				.UseConfiguration(configure: configBuilder =>
					configBuilder
						.EmbeddedSource<App>()
						.Section<AppConfig>()
				)

				// Enable localization (see appsettings.json for supported languages)
				.UseLocalization()

				// Register Json serializers (ISerializer and ISerializer)
				.UseSerialization()

				// Register services for the application
				.ConfigureServices(services =>
				{
					// TODO: Register your services
					//services.AddSingleton<IMyService, MyService>();
				})


				// Enable navigation, including registering views and viewmodels
				.UseNavigation(
//+:cnd:noEmit
#if(reactive)
			ReactiveViewModelMappings.ViewModelMappings,
#endif
//-:cnd:noEmit
			RegisterRoutes)

				// Add navigation support for toolkit controls such as TabBar and NavigationView
				.UseToolkitNavigation()

				.Build(enableUnoLogging: true);

	}
	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap(ViewModel: typeof(ShellViewModel)),
			new ViewMap<MainPage, MainViewModel>(),
			new DataViewMap<SecondPage, SecondViewModel, Entity>()
			);

		routes
			.Register(
				new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
						Nested: new RouteMap[]
						{
										new RouteMap("Main", View: views.FindByViewModel<MainViewModel>()),
										new RouteMap("Second", View: views.FindByViewModel<SecondViewModel>()),
						}));
	}
}
