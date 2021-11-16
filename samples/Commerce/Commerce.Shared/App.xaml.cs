﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.Extensions.Configuration;
using Uno.Extensions.Hosting;
using Uno.Extensions.Logging;
using Uno.Extensions.Logging.Serilog;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.Toolkit.Navigators;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Commerce.ViewModels;
using Commerce.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Commerce.Models;
using Uno.Extensions.Navigation.Toolkit;

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
#if DEBUG
			.UseEnvironment(Environments.Development)
#endif

			// Load configuration information from appsettings.json
			// Also load configuration from environment specific files if they exist eg appsettings.development.json
			// UseEmbeddedAppSettings<App>() if you want to include appsettings files as Embedded Resources instead of Content
			.UseAppSettings(includeEnvironmentSettings: true)

			//.UseLogging()
			//.ConfigureLogging(logBuilder =>
			//{
			//    logBuilder
			//         .SetMinimumLevel(LogLevel.Trace)
			//         .XamlLogLevel(LogLevel.Information)
			//         .XamlLayoutLogLevel(LogLevel.Information);
			//})
			//.UseSerilog(true, true)

			.UseConfigurationSectionInApp<AppInfo>()
			.UseSettings<CommerceSettings>()
			.ConfigureServices(services =>
			{
				services
				.AddSingleton<IProductService>(sp => new ProductService("products.json"))
				.AddSingleton<ICartService>(sp => new CartService("products.json"));
			})
			.UseNavigation(RegisterRoutes)
			.UseToolkitNavigation()
			.Build()
			.EnableUnoLogging();

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
			_window = Windows.UI.Xaml.Window.Current;
#endif

			var rootFrame = _window.Content as Frame;

			// Do not repeat app initialization when the Window already has content,
			// just ensure that the window is active
			if (rootFrame == null)
			{
				// Create a Frame to act as the navigation context and navigate to the first page
				//rootFrame = new Frame();
				rootFrame = new Frame().WithNavigation(Host.Services);

				rootFrame.NavigationFailed += OnNavigationFailed;

				if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
				{
					// TODO: Load state from previously suspended application
				}

				// Place the frame in the current Window
				_window.Content = rootFrame;
			}

#if !(NET5_0 && WINDOWS)
			if (args.PrelaunchActivated == false)
#endif
			{
				if (rootFrame.Content == null)
				{
					//// When the navigation stack isn't restored navigate to the first page,
					//// configuring the new page by passing required information as a navigation
					//// parameter
					//rootFrame.Navigate(typeof(MainPage), args.Arguments);
				}
				// Ensure the current window is active
				_window.Activate();
			}

			await Task.Run(async () =>
			{
				await Host.StartAsync();
			});

			var nav = Host.Services.GetService<INavigator>();
			var navResult = nav.NavigateToRouteAsync(this, "Login");
			//var navResult = nav.NavigateToRouteAsync(this, "/CommerceHomePage/Products/ProductDetails?ProductId=3");
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
				builder.SetMinimumLevel(LogLevel.Information);

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
				// builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

				// Debug JS interop
				// builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
			});

			global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;
		}

		private static void RegisterRoutes(IRouteBuilder builder)
		{
			builder.Register(new RouteMap("Login", typeof(LoginPage), typeof(LoginViewModel.BindableLoginViewModel)))
					.Register(new RouteMap("Home", typeof(CommerceHomePage),
						RegionInitialization: (region, nav) => nav.Route.Next().IsEmpty() ?
													nav with { Route = nav.Route.Append(Route.NestedRoute("Products")) } :
													nav))
					.Register(new RouteMap("Products", typeof(FrameView),
						ViewModel: typeof(ProductsViewModel.BindableProductsViewModel),
						RegionInitialization: (region, nav) => nav.Route.Next().IsEmpty() ?
												nav with { Route = nav.Route.AppendPage<ProductsPage>() } : nav with
												{
													Route = nav.Route.ContainsView<ProductsPage>() ?
																	nav.Route :
																	nav.Route.InsertPage<ProductsPage>()
												}))
					.Register(new RouteMap("Deals", typeof(FrameView),
						RegionInitialization: (region, nav) => nav.Route.IsEmpty() ?
												nav with { Route = nav.Route with { Base = "+DealsPage/HotDeals" } } :
												nav with { Route = nav.Route with { Path = "+DealsPage/HotDeals" } }))
					.Register(new RouteMap<Product>("ProductDetails",
						RegionInitialization: (region, nav) => (App.Current as App).Window.Content.ActualSize.X > 800 ?
												nav with { Route = nav.Route with { Scheme="./", Base="Details", Path = nameof(ProductDetailsPage) } } :
												nav with { Route = nav.Route with { Base = nameof(ProductDetailsPage) } }))
					.Register(new RouteMap<Product>(nameof(ProductDetailsPage),
						typeof(ProductDetailsPage),
						typeof(ProductDetailsViewModel.BindableProductDetailsViewModel),
						BuildQueryParameters: entity => new Dictionary<string, string> { { "ProductId", (entity as Product)?.ProductId + "" } }))
					.Register(new RouteMap(typeof(CartDialog).Name, typeof(CartDialog),
						RegionInitialization: (region, nav) => nav.Route.Next().IsEmpty() ?
												nav with { Route = nav.Route.AppendNested<CartPage>() } :
												nav))
					.Register(new RouteMap("Filter", typeof(FilterPopup), typeof(FilterViewModel.BindableFilterViewModel)))
					.Register(new RouteMap("Profile", typeof(ProfilePage), typeof(ProfileViewModel)))
          .Register(new RouteMap(typeof(CartDialog).Name, typeof(CartDialog),
				    RegionInitialization: (region, nav) => nav.Route.Next().IsEmpty() ?
										nav with { Route = nav.Route.AppendNested<CartPage>() } :
										nav))
			    .Register(new RouteMap(typeof(CartPage).Name, typeof(CartPage), typeof(CartViewModel)));
		}
	}
}
