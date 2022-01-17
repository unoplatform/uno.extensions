#define NO_REFLECTION // MessageDialog currently doesn't work with no-reflection set
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Playground.ViewModels;
using Playground.Views;
using Uno.Extensions.Hosting;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Toolkit;
using Uno.Extensions.Serialization;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace Playground
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public sealed partial class App : Application
	{
		private Window _window;
		public Window Window => _window;

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
#if NO_REFLECTION
					.UseNavigation(RegisterRoutes)
#else
					.UseNavigation()
#endif
					// Add navigation support for toolkit controls such as TabBar and NavigationView
					.UseToolkitNavigation()

#if NO_REFLECTION // Force the use of RouteResolver instead of RouteResolverDefault 
					.ConfigureServices(services =>
					{
						// Force the route resolver that doesn't use reflection
						services.AddSingleton<IRouteResolver, RouteResolver>();
					})
#endif
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
			_window = Window.Current;
#endif

			_window.Content = Host.Services.NavigationHost(
				// Option 1: This requires Shell to be the first RouteMap - best for perf as no reflection required
				// initialRoute: ""
				// Option 2: Specify route name
				// initialRoute: "Shell"
				// Option 3: Specify the view model. To avoid reflection, you can still define a routemap
				initialViewModel: typeof(ShellViewModel)
				);
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

		/// <summary>
		/// Configures global Uno Platform logging
		/// </summary>
		//        private static void InitializeLogging()
		//        {
		//            var factory = LoggerFactory.Create(builder =>
		//            {
		//#if __WASM__
		//                builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
		//#elif __IOS__
		//                builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
		//#elif NETFX_CORE
		//                builder.AddDebug();
		//#else
		//                builder.AddConsole();
		//#endif

		//                // Exclude logs below this level
		//                builder.SetMinimumLevel(LogLevel.Information);

		//                // Default filters for Uno Platform namespaces
		//                builder.AddFilter("Uno", LogLevel.Warning);
		//                builder.AddFilter("Windows", LogLevel.Warning);
		//                builder.AddFilter("Microsoft", LogLevel.Warning);

		//                // Generic Xaml events
		//                // builder.AddFilter("Windows.UI.Xaml", LogLevel.Debug );
		//                // builder.AddFilter("Windows.UI.Xaml.VisualStateGroup", LogLevel.Debug );
		//                // builder.AddFilter("Windows.UI.Xaml.StateTriggerBase", LogLevel.Debug );
		//                // builder.AddFilter("Windows.UI.Xaml.UIElement", LogLevel.Debug );
		//                // builder.AddFilter("Windows.UI.Xaml.FrameworkElement", LogLevel.Trace );

		//                // Layouter specific messages
		//                // builder.AddFilter("Windows.UI.Xaml.Controls", LogLevel.Debug );
		//                // builder.AddFilter("Windows.UI.Xaml.Controls.Layouter", LogLevel.Debug );
		//                // builder.AddFilter("Windows.UI.Xaml.Controls.Panel", LogLevel.Debug );

		//                // builder.AddFilter("Windows.Storage", LogLevel.Debug );

		//                // Binding related messages
		//                // builder.AddFilter("Windows.UI.Xaml.Data", LogLevel.Debug );
		//                // builder.AddFilter("Windows.UI.Xaml.Data", LogLevel.Debug );

		//                // Binder memory references tracking
		//                // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

		//                // RemoteControl and HotReload related
		//                // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

		//                // Debug JS interop
		//                // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
		//            });

		//            global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

		//#if HAS_UNO
		//			global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
		//#endif
		//        }

		private static void RegisterRoutes(IRouteRegistry routes)
		{
			// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
			routes.Register(new RouteMap("Shell", ViewModel: typeof(ShellViewModel),
				Nested: new[]
				{
					new RouteMap("Home", View: typeof(HomePage)),
					new RouteMap("Second", View: typeof(SecondPage)),
					new RouteMap("Third", View: typeof(ThirdPage)),
					new RouteMap("Fourth", View: typeof(FourthPage), ViewModel: typeof(FourthViewModel)),
					new RouteMap("Fifth", View: typeof(FifthPage), ViewModel: typeof(FifthViewModel)),
					new RouteMap("Dialogs", View: typeof(DialogsPage),
					Nested: new[]
					{
						new RouteMap("Simple", View:typeof(SimpleDialog)),
						new RouteMap("Complex", View:typeof(ComplexDialog),
						Nested: new[]
						{
							new RouteMap("ComplexDialogFirst", View:typeof(ComplexDialogFirstPage)),
							new RouteMap("ComplexDialogSecond", View:typeof(ComplexDialogSecondPage))
						})
					}),
					new RouteMap("VisualStates", View: typeof(VisualStatesPage)),
				}));
		}
	}
}
