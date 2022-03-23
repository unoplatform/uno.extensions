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
using Uno.Extensions.Navigation.Toolkit.Controls;

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

		private IHost _host;

		public App()
		{
			_host = UnoHost
					.CreateDefaultBuilder()
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

			var notif = _host.Services.GetService<IRouteNotifier>();
			notif.RouteChanged += RouteUpdated;


			_window.Content = _host.Services.NavigationHost();
			_window.Activate();

			await Task.Run(async () =>
			{
				await _host.StartAsync();
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

		private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
		{
			var forgotPasswordDialog = new MessageDialogViewMap(
					Content: "Click OK, or Cancel",
					Title: "Forgot your password!",
					DelayUserInput: true,
					DefaultButtonIndex: 1,
					Buttons:new DialogAction[]
					{
						new(Label: "Yeh!",Id:"Y"),
						new(Label: "Nah", Id:"N")
					}
				);

			views.Register(
				new ViewMap(ViewModel: typeof(ShellViewModel)),
				new ViewMap(View: typeof(LoginPage), ViewModel: typeof(LoginViewModel.BindableLoginViewModel), ResultData: typeof(Credentials)),
				new ViewMap(View: typeof(HomePage), Data: new DataMap<Credentials>()),
				new ViewMap(View: typeof(ProductsPage), ViewModel: typeof(ProductsViewModel.BindableProductsViewModel)),
				new ViewMap(DynamicView: () =>
						   (App.Current as App)?.Window?.Content?.ActualSize.X > 800 ? typeof(ProductControl) : typeof(ProductDetailsPage),
							ViewModel: typeof(ProductDetailsViewModel.BindableProductDetailsViewModel), Data: new DataMap<Product>(
																						ToQuery: product => new Dictionary<string, string> { { nameof(Product.ProductId), product.ProductId.ToString() } },
																						FromQuery: async (sp, query) =>
																						{
																							var id = int.Parse(query[nameof(Product.ProductId)]);
																							var ps = sp.GetRequiredService<IProductService>();
																							var products = await ps.GetProducts(default, default);
																							return products.FirstOrDefault(p => p.ProductId == id);
																						})),
				new ViewMap(View: typeof(FilterPage), ViewModel: typeof(FiltersViewModel.BindableFiltersViewModel), Data: new DataMap<Filters>()),
				new ViewMap(View: typeof(DealsPage), ViewModel: typeof(DealsViewModel)),
				new ViewMap(View: typeof(ProfilePage), ViewModel: typeof(ProfileViewModel)),
				new ViewMap(View: typeof(CartPage), ViewModel: typeof(CartViewModel)),
				new ViewMap(View: typeof(ProductDetailsPage), ViewModel: typeof(CartProductDetailsViewModel.BindableCartProductDetailsViewModel), Data: new DataMap<CartItem>(
																						ToQuery: cartItem => new Dictionary<string, string> {
																							{ nameof(Product.ProductId), cartItem.Product.ProductId.ToString() },
																							{ nameof(CartItem.Quantity),cartItem.Quantity.ToString() } },
																						FromQuery: async (sp, query) =>
																						{
																							var id = int.Parse(query[nameof(Product.ProductId)]);
																							var quantity = int.Parse(query[nameof(CartItem.Quantity)]);
																							var ps = sp.GetRequiredService<IProductService>();
																							var products = await ps.GetProducts(default, default);
																							var p = products.FirstOrDefault(p => p.ProductId == id);
																							return new CartItem(p, quantity);
																						})),
				new ViewMap(View: typeof(CheckoutPage)),
				forgotPasswordDialog
				);

			routes
				.Register(
				views =>
					new("", View: views.FindByViewModel<ShellViewModel>(), // IsPrivate: true,
							Nested: new RouteMap[]
							{
								new("Login", View: views.FindByResultData<Credentials>(),
										Nested: new RouteMap[]
										{
											new ("Forgot", forgotPasswordDialog)
										}),
								new RouteMap("Home", View: views.FindByData<Credentials>(),
										Nested: new RouteMap[]{
											new ("Products",
													View: views.FindByViewModel<ProductsViewModel.BindableProductsViewModel>(),
													IsDefault: true,
													Nested: new  RouteMap[]{
														new RouteMap("Filter",  View: views.FindByViewModel<FiltersViewModel.BindableFiltersViewModel>())
													}),
											new("Product",
													View: views.FindByViewModel<ProductDetailsViewModel.BindableProductDetailsViewModel>(),
													DependsOn:"Products"),
												//	Init: req => (App.Current as App).Window.Content.ActualSize.X > 800 ?
												//req with { Route = req.Route with {Base = "Details",Path="Products"} } :
												//req),

											new("Deals", View:views.FindByViewModel<DealsViewModel>()),

											new("Profile", View:views.FindByViewModel<ProfileViewModel>()),

											new("Cart", View: views.FindByViewModel<CartViewModel>(),
													Nested: new []{
														new RouteMap("CartDetails",View: views.FindByViewModel<CartProductDetailsViewModel.BindableCartProductDetailsViewModel>()),
														new RouteMap("Checkout", View: views.FindByView<CheckoutPage>())
													})
											})
							}));

			;
		}

		public async void RouteUpdated(object sender, RouteChangedEventArgs e)
		{
			try
			{
				var rootRegion = e.Region.Root();
				var route = rootRegion.GetRoute();


#if !__WASM__ && !WINUI
				CoreApplication.MainView?.DispatcherQueue.TryEnqueue(() =>
				{
					var appTitle = ApplicationView.GetForCurrentView();
					appTitle.Title = "Commerce: " + (route + "").Replace("+", "/");
				});
#endif


//#if __WASM__
//				// Note: This is a hack to avoid error being thrown when loading products async
//				await Task.Delay(1000).ConfigureAwait(false);
//				CoreApplication.MainView?.DispatcherQueue.TryEnqueue(() =>
//				{
//					var href = WebAssemblyRuntime.InvokeJS("window.location.href");
//					var url = new UriBuilder(href);
//					url.Query = route.Query();
//					url.Path = route.FullPath()?.Replace("+", "/");
//					var webUri = url.Uri.OriginalString;
//					var js = $"window.history.pushState(\"{webUri}\",\"\", \"{webUri}\");";
//					Console.WriteLine($"JS:{js}");
//					var result = WebAssemblyRuntime.InvokeJS(js);
//				});
//#endif
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
			}
		}
	}
}
