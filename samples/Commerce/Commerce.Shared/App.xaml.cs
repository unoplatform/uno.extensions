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
using Uno.Extensions.Logging.Serilog;
using Uno.Extensions.Serialization;

namespace Commerce
{
	public sealed partial class App : Application
	{
		private Window _window;
		public Window Window => _window;

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

#if NET5_0 && WINDOWS
            _window = new Window();
            _window.Activate();
#else
			_window = Windows.UI.Xaml.Window.Current;
#endif

			var rootFrame = new Frame();
			_window.Content = rootFrame;

			rootFrame.Navigate(typeof(LoginPage));

			_window.Activate();
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
	}
}
