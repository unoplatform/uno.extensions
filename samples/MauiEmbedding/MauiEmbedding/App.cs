using CommunityToolkit.Maui;
using Microsoft.Maui;
//using Telerik.Maui.Controls;
using MControls = Microsoft.Maui.Controls;
using MauiControlsExternal;

namespace MauiEmbedding;

public class App : Application
{
	protected Window? MainWindow { get; private set; }
	protected IHost? Host { get; private set; }

	public App()
	{
		UnoMusicApp.Helpers.ThreadHelpers.Init(SynchronizationContext.Current!);
	}

	protected async override void OnLaunched(LaunchActivatedEventArgs args)
	{
		var builder = this.CreateBuilder(args)
			.UseMauiEmbedding(maui =>
			{

				maui
				.UseMauiCommunityToolkit()
				//.UseTelerikControls()
				.UseCustomLibrary();

				Microsoft.Maui.Handlers.ShapeViewHandler.Mapper.AppendToMapping("BackgroundColor", (h, v) =>
				{
					if (v is MControls.BoxView boxview)
					{
						boxview.Background = MControls.Brush.Fuchsia;
						Microsoft.Maui.Handlers.ShapeViewHandler.MapBackground(h, boxview);
					}
				});
			})
			// Add navigation support for toolkit controls such as TabBar and NavigationView
			.UseToolkitNavigation()
			.Configure(host => host
#if DEBUG
				// Switch to Development environment when running in DEBUG
				.UseEnvironment(Environments.Development)
#endif
				.UseLogging(configure: (context, logBuilder) =>
				{
					// Configure log levels for different categories of logging
					logBuilder
						.SetMinimumLevel(
							context.HostingEnvironment.IsDevelopment() ?
								LogLevel.Information :
								LogLevel.Warning)

						// Default filters for core Uno Platform namespaces
						.CoreLogLevel(LogLevel.Warning);

					// Uno Platform namespace filter groups
					// Uncomment individual methods to see more detailed logging
					//// Generic Xaml events
					//logBuilder.XamlLogLevel(LogLevel.Debug);
					//// Layouter specific messages
					//logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
					//// Storage messages
					//logBuilder.StorageLogLevel(LogLevel.Debug);
					//// Binding related messages
					//logBuilder.XamlBindingLogLevel(LogLevel.Debug);
					//// Binder memory references tracking
					//logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
					//// RemoteControl and HotReload related
					//logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
					//// Debug JS interop
					//logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

				}, enableUnoLogging: true)
				.UseSerilog(consoleLoggingEnabled: true, fileLoggingEnabled: true)
				.UseConfiguration(configure: configBuilder =>
					configBuilder
						.EmbeddedSource<App>()
						.Section<AppConfig>()
				)
				// Enable localization (see appsettings.json for supported languages)
				.UseLocalization()
				.ConfigureServices((context, services) =>
				{
					// TODO: Register your services
					//services.AddSingleton<IMyService, MyService>();
				})
				.UseNavigation(RegisterRoutes)
			);
		MainWindow = builder.Window;

		Host = await builder.NavigateAsync<Shell>();
	}

	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap(ViewModel: typeof(ShellViewModel)),
			new ViewMap<MainPage, MainViewModel>(),
			new ViewMap<MauiControlsPage, MauiControlsViewModel>(),
			new ViewMap<MauiEssentialsPage, MauiEssentialsViewModel>(),
			new ViewMap<TelerikControlsPage, TelerikControlsViewModel>(),
			new ViewMap<MauiColorsPage, MauiColorsViewModel>(),
			new ViewMap<ExternalLibPage, ExternalLibViewModel>(),
			new ViewMap<MCTControlsPage, MCTControlsViewModel>()
		);

		routes.Register(
			new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
				Nested: new RouteMap[]
				{
					new RouteMap("Main", View: views.FindByViewModel<MainViewModel>()),
					new RouteMap(nameof(MauiControlsViewModel), View: views.FindByViewModel<MauiControlsViewModel>()),
					new RouteMap(nameof(MCTControlsViewModel), View: views.FindByViewModel<MCTControlsViewModel>()),
					new RouteMap(nameof(MauiEssentialsViewModel), View: views.FindByViewModel<MauiEssentialsViewModel>()),
					new RouteMap(nameof(MauiColorsViewModel), View: views.FindByViewModel<MauiColorsViewModel>()),
					new RouteMap(nameof(ExternalLibPage), View: views.FindByViewModel<ExternalLibViewModel>()),
					new RouteMap(nameof(TelerikControlsViewModel), View: views.FindByViewModel<TelerikControlsViewModel>()),
				}
			)
		);
	}
}
