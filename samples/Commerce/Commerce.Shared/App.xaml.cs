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
using Uno.Extensions.Navigation.UI;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.Toolkit;
using Uno.Extensions.Serialization;
using Uno.Foundation;
using Commerce.Views;
using Uno.Extensions.Logging.Serilog;
using Uno.Extensions.Navigation.UI.Controls;

#if WINUI
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
using Window = Microsoft.UI.Xaml.Window;
using CoreApplication = Windows.ApplicationModel.Core.CoreApplication;
#else
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
using Window = Windows.UI.Xaml.Window;
using CoreApplication = Windows.ApplicationModel.Core.CoreApplication;
#endif

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

					// Register entities for saving settings
					.UseSettings<Credentials>()


					// Register Json serializers (ISerializer and IStreamSerializer)
					.UseSerialization()

					// Register services for the application
					.ConfigureServices(services =>
					{
						services

							.AddSingleton<IProductService, ProductService>()
							.AddSingleton<ICartService, CartService>()
							.AddSingleton<IDealService, DealService>()
							.AddSingleton<IProfileService, ProfileService>();
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
			_window = Window.Current;
#endif

			var notif = Host.Services.GetService<IRouteNotifier>();
			notif.RouteChanged += RouteUpdated;


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

		private static void RegisterRoutes(IRouteRegistry routes)//, IViewRegistry views)
		{
			routes
				//.Register(new(nameof(ShellView), typeof(ShellView)))
				//.Register(new("Login", typeof(LoginPage)))
				//.Register(new("Home", typeof(HomePage),
				//	Init: nav => nav.Route.Next().IsEmpty() ?
				//								nav with { Route = nav.Route.Append(Route.NestedRoute("Products")) } :
				//								nav))
				//.Register(new("Products", typeof(FrameView),
				//	Init: nav => nav.Route.Next().IsEmpty() ?
				//								nav with { Route = nav.Route.AppendPage<ProductsPage>() } : nav with
				//								{
				//									Route = nav.Route.ContainsView<ProductsPage>() ?
				//													nav.Route :
				//													nav.Route.InsertPage<ProductsPage>()
				//								}))
				//.Register(new(nameof(ProductsPage), typeof(ProductsPage)))
				//.Register(new("Deals", typeof(DealsPage),
				//	Init: nav => nav.Route.Next().IsEmpty() ?
				//								nav with { Route = nav.Route.AppendPage<DealsPage>() } : nav with
				//								{
				//									Route = nav.Route.ContainsView<DealsPage>() ?
				//													nav.Route :
				//													nav.Route.InsertPage<DealsPage>()
				//								}))
				//.Register(new("ProductDetails", typeof(ProductDetailsPage),
				//	Init: nav => (App.Current as App).Window.Content.ActualSize.X > 800 ?
				//								nav with { Route = nav.Route with { Scheme = "./", Base = "Details", Path = nameof(ProductDetailsPage) } } :
				//								nav with { Route = nav.Route with { Base = nameof(ProductDetailsPage) } }))
				//.Register(new("Details"))
				//.Register(new(nameof(ProductDetailsPage), typeof(ProductDetailsPage)))
				//.Register(new(nameof(CartFlyout), typeof(CartFlyout),
				//	Init: nav => nav.Route.Next().IsEmpty() ?
				//									nav with { Route = nav.Route.AppendNested<CartPage>() } :
				//									nav))
				//.Register(new(nameof(CartPage), typeof(CartPage)))
				//.Register(new("Checkout", typeof(CheckoutPage)))
				//.Register(new("Filter", typeof(FilterFlyout),
				//	Init: nav => nav.Route.Next().IsEmpty() ?
				//								nav with { Route = nav.Route.AppendPage<FilterPage>() } : nav with
				//								{
				//									Route = nav.Route.ContainsView<FilterPage>() ?
				//													nav.Route :
				//													nav.Route.InsertPage<FilterPage>()
				//								}))
				//.Register(new(nameof(FilterPage), typeof(FilterPage)))
				//.Register(new("Profile", typeof(ProfilePage)))

				//////////////////////////////////// POC ////////////////////////////////////

				.Register(
					new("Shell", ViewModel: typeof(ShellViewModel),
							Nested: new RouteMap[]
							{
								new("Login", View: typeof(LoginPage), ViewModel: typeof(LoginViewModel.BindableLoginViewModel), ResultData: typeof(Credentials)),
								new RouteMap<Credentials>("Home", View: typeof(HomePage),
										Nested: new RouteMap[]{
											new ("Products", View: typeof(ProductsPage), ViewModel: typeof(ProductsViewModel.BindableProductsViewModel),
															IsDefault: true,
															Nested: new  RouteMap[]{
																new RouteMap<Product>("Details", View: typeof(ProductDetailsPage), ViewModel: typeof(ProductDetailsViewModel.BindableProductDetailsViewModel),
																						ToQuery: product => new Dictionary<string, string> { { nameof(Product.ProductId), product.ProductId.ToString() } },
																						FromQuery: async (sp, query) => {
																							var id = int.Parse(query[nameof(Product.ProductId)]);
																							var ps = sp.GetRequiredService<IProductService>();
																							var products = await ps.GetProducts(default, default);
																							return products.FirstOrDefault(p=>p.ProductId==id);
																						}),
																new RouteMap<Filters, Filters>("Filter", View: typeof(FilterPage), ViewModel: typeof(FiltersViewModel.BindableFiltersViewModel))
															}),

											new("Deals", View: typeof(DealsPage), ViewModel: typeof(DealsViewModel)),

											new("Profile", View: typeof(ProfilePage), ViewModel: typeof(ProfileViewModel)),

											new("Cart", View: typeof(CartPage), ViewModel: typeof(CartViewModel),
													Nested: new []{
														new RouteMap<CartItem>("CartDetails", View: typeof(ProductDetailsPage), ViewModel: typeof(CartProductDetailsViewModel.BindableCartProductDetailsViewModel),
																						ToQuery: cartItem => new Dictionary<string, string> {
																							{ nameof(Product.ProductId), cartItem.Product.ProductId.ToString() },
																							{ nameof(CartItem.Quantity),cartItem.Quantity.ToString() } },
																						FromQuery: async (sp, query) => {
																							var id = int.Parse(query[nameof(Product.ProductId)]);
																							var quantity = int.Parse(query[nameof(CartItem.Quantity)]);
																							var ps = sp.GetRequiredService<IProductService>();
																							var products = await ps.GetProducts(default, default);
																							var p = products.FirstOrDefault(p=>p.ProductId==id);
																							return new CartItem(p,quantity);
																						}),
														new RouteMap("Checkout", View: typeof(CheckoutPage))
													})
											})
							}));

			;

			//views
			//	.Register(new ViewMap(typeof(FrameView)))
			//	.Register(new ViewMap(typeof(FilterFlyout)))
			//	.Register(new ViewMap(typeof(CartFlyout)))
			//	.Register(new ViewMap(typeof(ShellView), typeof(ShellViewModel)))
			//	.Register(new ViewMap(typeof(LoginPage), typeof(LoginViewModel.BindableLoginViewModel)))
			//	.Register(new ViewMap(typeof(HomePage)))
			//	.Register(new ViewMap(typeof(ProductsPage), typeof(ProductsViewModel.BindableProductsViewModel)))
			//	.Register(new ViewMap(typeof(DealsPage), typeof(DealsViewModel)))
			//	.Register(new ViewMap<Product>(typeof(ProductDetailsPage), typeof(ProductDetailsViewModel.BindableProductDetailsViewModel),
			//		BuildQuery: product => new Dictionary<string, string> { { nameof(Product.ProductId), product.ProductId + "" } }))
			//	.Register(new ViewMap<Filters>(typeof(FilterPage), typeof(FiltersViewModel.BindableFiltersViewModel)))
			//	.Register(new ViewMap(typeof(ProfilePage), typeof(ProfileViewModel)))
			//	.Register(new ViewMap(typeof(CartPage), typeof(CartViewModel)))
			//	.Register(new ViewMap(typeof(CheckoutPage)));



		}

		public async void RouteUpdated(object sender, EventArgs e)
		{
			try
			{
				var reg = Host.Services.GetService<IRegion>();
				var rootRegion = reg.Root();
				var route = rootRegion.GetRoute();


#if !__WASM__ && !WINUI
				CoreApplication.MainView?.DispatcherQueue.TryEnqueue(() =>
				{
					var appTitle = ApplicationView.GetForCurrentView();
					appTitle.Title = "Commerce: " + (route + "").Replace("+", "/");
				});
#endif


#if __WASM__
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
