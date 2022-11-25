namespace MyExtensionsApp;

public static class HostConfiguration
{
	public static IApplicationBuilder Environment(this IApplicationBuilder builder)
	{
#if DEBUG
		// Switch to Development environment when running in DEBUG
		builder.Configure(host => host.UseEnvironment(Environments.Development));
#endif
		return builder;
	}

	public static IApplicationBuilder Logging(this IApplicationBuilder builder)
	{
		// Add platform specific log providers
		return builder.Configure(host => host.UseLogging(configure: (context, logBuilder) =>
		{
			// Configure log levels for different categories of logging
			logBuilder.SetMinimumLevel(
				context.HostingEnvironment.IsDevelopment() ?
					LogLevel.Information :
					LogLevel.Warning);
		}, enableUnoLogging: true));
	}

	public static IApplicationBuilder Configuration(this IApplicationBuilder builder)
	{
		return builder.Configure(host => host.UseConfiguration(configure: configBuilder =>
			configBuilder
				.EmbeddedSource<App>()
				.Section<AppConfig>()
		));
	}

	public static IApplicationBuilder Localization(this IApplicationBuilder builder)
	{
		// Enable localization (see appsettings.json for supported languages)
		return builder.Configure(host => host.UseLocalization());
	}

	public static IApplicationBuilder Serialization(this IApplicationBuilder builder)
	{
		// Register Json serializers (ISerializer and ISerializer)
		return builder.Configure(host => host.UseSerialization());
	}

	public static IApplicationBuilder Services(this IApplicationBuilder builder)
	{
		// Register services for the application
		return builder.Configure(host => host.ConfigureServices(services => {
			// TODO: Register your services
			//services.AddSingleton<IMyService, MyService>();
		}));
	}

	public static IApplicationBuilder Navigation(this IApplicationBuilder builder)
	{
		// Enable navigation, including registering views and viewmodels
		return builder
			.Configure(host => host.UseNavigation(
//+:cnd:noEmit
#if(reactive)
			ReactiveViewModelMappings.ViewModelMappings,
#endif
//-:cnd:noEmit
			RegisterRoutes))
			// Add navigation support for toolkit controls such as TabBar and NavigationView
			.UseToolkitNavigation();
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
