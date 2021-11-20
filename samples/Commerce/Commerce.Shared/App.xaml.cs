using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Commerce.Models;
using Commerce.Services;
using Commerce.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Extensions.Configuration;
using Uno.Extensions.Hosting;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.Toolkit;
using Uno.Foundation;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.Core;
using Commerce.Views;

namespace Commerce
{
	public sealed partial class App : Application
	{
		private Window _window;
		public Window Window => _window;

		private IHost Host { get; }

		public App()
		{
			Host = UnoHost
			.CreateDefaultBuilder(true)
#if false //DEBUG
			.UseEnvironment(Environments.Development)
#endif

			// Load configuration information from appsettings.json
			// Also load configuration from environment specific files if they exist eg appsettings.development.json
			// UseEmbeddedAppSettings<App>() if you want to include appsettings files as Embedded Resources instead of Content
			.UseAppSettings(includeEnvironmentSettings: true)

			//.UseLogging()
			//.ConfigureLogging(logBuilder =>
			//{
			//	logBuilder
			//		 .SetMinimumLevel(LogLevel.Trace)
			//		 .XamlLogLevel(LogLevel.Information)
			//		 .XamlLayoutLogLevel(LogLevel.Information);
			//})
			//.UseSerilog(true, true)

			.UseConfigurationSectionInApp<AppInfo>()
			.UseSettings<CommerceSettings>()
			.UseSettings<Credentials>()
			.ConfigureServices(services =>
			{
				services
				.AddSingleton<IProductService>(sp => new ProductService("products.json", "reviews.json"))
				.AddSingleton<ICartService>(sp => new CartService("products.json"))
				.AddSingleton<IProfileService>(sp => new ProfileService());
			})
			.UseNavigation(RegisterRoutes)
			.UseToolkitNavigation()
			.Build()
			.EnableUnoLogging();

			InitializeLogging();

			this.InitializeComponent();
			
			InitializeLogging();

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
			_window = Windows.UI.Xaml.Window.Current;
#endif

			var notif = Host.Services.GetService<IRouteNotifier>();
			notif.RouteChanged += RouteUpdated;


			_window.Content = new ShellView().WithNavigation(Host.Services);
			_window.Activate();

			await Task.Run(async () =>
			{
				await Host.StartAsync();
			});

			var nav = Host.Services.GetService<INavigator>();
#if __WASM__
			Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
			Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += AppGoBack;

#endif

		}

		public async void AppGoBack(object? sender, BackRequestedEventArgs e)
		{
			var backnav = Host.Services.GetService<INavigator>();
			var appTitle = ApplicationView.GetForCurrentView();
			appTitle.Title = "Back pressed - " + DateTime.Now.ToString("HH:mm:ss");
			var response = await backnav.GoBack(this);
			//e.Handled = response.Success;

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
		private static void InitializeLogging()
		{
			var factory = LoggerFactory.Create(builder =>
			{
#if __WASM__
				builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
                builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#elif NETFX_CORE
				builder.AddDebug();
#else
                builder.AddConsole();
#endif

				// Exclude logs below this level
				builder.SetMinimumLevel(LogLevel.Trace);

				// Default filters for Uno Platform namespaces
				builder.AddFilter("Uno", LogLevel.Warning);
				builder.AddFilter("Windows", LogLevel.Warning);
				builder.AddFilter("Microsoft", LogLevel.Warning);

				// Generic Xaml events
				// builder.AddFilter("Windows.UI.Xaml", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.VisualStateGroup", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.StateTriggerBase", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.UIElement", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.FrameworkElement", LogLevel.Trace );

				// Layouter specific messages
				// builder.AddFilter("Windows.UI.Xaml.Controls", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.Controls.Layouter", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.Controls.Panel", LogLevel.Debug );

				// builder.AddFilter("Windows.Storage", LogLevel.Debug );

				// Binding related messages
				// builder.AddFilter("Windows.UI.Xaml.Data", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.Data", LogLevel.Debug );

				// Binder memory references tracking
				// builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

				// RemoteControl and HotReload related
				builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Trace);

				// Debug JS interop
				// builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
			});

			global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

			Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
		}

		private static void RegisterRoutes(IRouteBuilder builder)
		{
			builder
				.Register(new RouteMap(nameof(ShellView), typeof(ShellView), typeof(ShellViewModel)))
				.Register(new RouteMap("Login", typeof(LoginPage), typeof(LoginViewModel.BindableLoginViewModel)))
					.Register(new RouteMap("Home", typeof(HomePage), typeof(HomeViewModel),
						RegionInitialization: (region, nav) => nav.Route.Next().IsEmpty() ?
													nav with { Route = nav.Route.Append(Route.NestedRoute("Products")) } :
													nav))
					.Register(new RouteMap("Products", typeof(FrameView),
						RegionInitialization: (region, nav) => nav.Route.Next().IsEmpty() ?
												nav with { Route = nav.Route.AppendPage<ProductsPage>() } : nav with
												{
													Route = nav.Route.ContainsView<ProductsPage>() ?
																	nav.Route :
																	nav.Route.InsertPage<ProductsPage>()
												}))
					.Register(new RouteMap(nameof(ProductsPage), typeof(ProductsPage),
						ViewModel: typeof(ProductsViewModel.BindableProductsViewModel)))
					.Register(new RouteMap("Deals", typeof(FrameView),
						RegionInitialization: (region, nav) => nav.Route.IsEmpty() ?
												nav with { Route = nav.Route with { Base = "+DealsPage/HotDeals" } } :
												nav with { Route = nav.Route with { Path = "+DealsPage/HotDeals" } }))
					.Register(new RouteMap<Product>("ProductDetails",
						RegionInitialization: (region, nav) => (App.Current as App).Window.Content.ActualSize.X > 800 ?
												nav with { Route = nav.Route with { Scheme = "./", Base = "Details", Path = nameof(ProductDetailsPage) } } :
												nav with { Route = nav.Route with { Base = nameof(ProductDetailsPage) } }))
					.Register(new RouteMap<Product>(nameof(ProductDetailsPage),
						typeof(ProductDetailsPage),
						typeof(ProductDetailsViewModel.BindableProductDetailsViewModel),
						BuildQueryParameters: entity => new Dictionary<string, string> { { "ProductId", (entity as Product)?.ProductId + "" } }))
					.Register(new RouteMap(typeof(CartDialog).Name, typeof(CartDialog),
						RegionInitialization: (region, nav) => nav.Route.Next().IsEmpty() ?
												nav with { Route = nav.Route.AppendNested<CartPage>() } :
												nav))
					.Register(new RouteMap<Filters>("Filter", typeof(FilterPopup), typeof(FiltersViewModel.BindableFiltersViewModel)))
					.Register(new RouteMap("Profile", typeof(ProfilePage), typeof(ProfileViewModel)))
		  .Register(new RouteMap(typeof(CartDialog).Name, typeof(CartDialog),
					RegionInitialization: (region, nav) => nav.Route.Next().IsEmpty() ?
										nav with { Route = nav.Route.AppendNested<CartPage>() } :
										nav))
				.Register(new RouteMap(typeof(CartPage).Name, typeof(CartPage), typeof(CartViewModel)));
		}

		public async void RouteUpdated(object sender, EventArgs e)
		{
			try
			{
				var reg = Host.Services.GetService<IRegion>();
				var rootRegion = reg.Root();
				var route = rootRegion.GetRoute();


#if !__WASM__
				var appTitle = ApplicationView.GetForCurrentView();
				appTitle.Title = "Commerce: " + (route + "").Replace("+", "/");

#else
				// Note: This is a hack to avoid error being thrown when loading products async
				await Task.Delay(1000).ConfigureAwait(false);
				CoreApplication.MainView?.DispatcherQueue.TryEnqueue(() =>
				{
					var href = WebAssemblyRuntime.InvokeJS("window.location.href");
					var url = new UriBuilder(href);
					url.Query = route.Query();
					url.Path = route.FullPath()?.Replace("+", "/");
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
}
