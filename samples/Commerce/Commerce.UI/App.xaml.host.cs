using System.Collections.Generic;
using System.Linq;
using Commerce.Data.Models;
using Commerce.Business.Models;
using Commerce.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Configuration;
using Uno.Extensions.Hosting;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Toolkit;
using Uno.Extensions.Serialization;
using Commerce.Views;
using Commerce.Reactive;

#if WINUI
using Microsoft.UI.Xaml;
#else
using Windows.UI.Xaml;
#endif


namespace Commerce
{
	public sealed partial class App : Application
	{
		private readonly IHost _host = BuildAppHost();

		private static IHost BuildAppHost()
		{
			return UnoHost
					.CreateDefaultBuilder()
#if DEBUG
					// Switch to Development environment when running in DEBUG
					.UseEnvironment(Environments.Development)
#endif


					// Add platform specific log providers
					.UseLogging(configure: logBuilder =>
					{
						// Configure log levels for different categories of logging
						logBuilder
								.SetMinimumLevel(LogLevel.Information)
								.XamlLogLevel(LogLevel.Information)
								.XamlLayoutLogLevel(LogLevel.Information)
								.AddFilter("Uno.Extensions.Navigation", LogLevel.Trace);
					})

					.UseConfiguration(configure: configBuilder=>
						configBuilder
							.ContentSource()
							.Section<AppInfo>()
							.Section<Credentials>()
					)


					// Register Json serializers (ISerializer and ISerializer)
					.UseSerialization()

					// Register services for the application
					.ConfigureServices(services =>
					{
						services
							.AddSingleton<ICartEndpoint, CartEndpoint>()
							.AddSingleton<IProductsEndpoint, ProductsEndpoint>()

							.AddSingleton<ICartService, CartService>()
							.AddSingleton<IDealService, DealService>()
							.AddSingleton<IProductService, ProductService>()
							.AddSingleton<IProfileService, ProfileService>();
					})


					// Enable navigation, including registering views and viewmodels
					.UseNavigation(
						RegisterRoutes,
						createViewRegistry: sc => new ReactiveViewRegistry(sc, ReactiveViewModelMappings.ViewModelMappings),
						configure: cfg => cfg with { AddressBarUpdateEnabled = true })
					.ConfigureServices(services =>
					{
						services
							.AddSingleton<IRouteResolver, ReactiveRouteResolver>();
					})
					// Add navigation support for toolkit controls such as TabBar and NavigationView
					.UseToolkitNavigation()


					.Build(enableUnoLogging: true);

			
		}

		private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
		{
			var forgotPasswordDialog = new MessageDialogViewMap(
								Content: "Click OK, or Cancel",
								Title: "Forgot your password!",
								DelayUserInput: true,
								DefaultButtonIndex: 1,
								Buttons: new DialogAction[]
								{
								new(Label: "Yeh!",Id:"Y"),
								new(Label: "Nah", Id:"N")
								}
							);

			views.Register(
					new ViewMap(ViewModel: typeof(ShellViewModel)),
					new ViewMap(View: typeof(LoginPage), ViewModel: typeof(LoginViewModel), ResultData: typeof(Credentials)),
					new ViewMap(View: typeof(HomePage), Data: new DataMap<Credentials>()),
					new ViewMap(View: typeof(ProductsPage), ViewModel: typeof(ProductsViewModel)),
					new ViewMap(ViewSelector: () =>
							   (App.Current as App)?.Window?.Content?.ActualSize.X > 800 ? typeof(ProductControl) : typeof(ProductDetailsPage),
								ViewModel: typeof(ProductDetailsViewModel), Data: new DataMap<Product>(
																							ToQuery: product => new Dictionary<string, string> { { nameof(Product.ProductId), product.ProductId.ToString() } },
																							FromQuery: async (sp, query) =>
																							{
																								var id = int.Parse(query[nameof(Product.ProductId)] + string.Empty);
																								var ps = sp.GetRequiredService<IProductService>();
																								var products = await ps.GetAll(default);
																								return products.FirstOrDefault(p => p.ProductId == id);
																							})),
					new ViewMap(View: typeof(FilterPage), ViewModel: typeof(FiltersViewModel), Data: new DataMap<Filters>()),
					new ViewMap(View: typeof(DealsPage), ViewModel: typeof(DealsViewModel)),
					new ViewMap(View: typeof(ProfilePage), ViewModel: typeof(ProfileViewModel)),
					new ViewMap(View: typeof(CartPage), ViewModel: typeof(CartViewModel)),
					new ViewMap(View: typeof(ProductDetailsPage), ViewModel: typeof(CartProductDetailsViewModel), Data: new DataMap<CartItem>(
																							ToQuery: cartItem => new Dictionary<string, string> {
																							{ nameof(Product.ProductId), cartItem.Product.ProductId.ToString() },
																							{ nameof(CartItem.Quantity),cartItem.Quantity.ToString() } },
																							FromQuery: async (sp, query) =>
																							{
																								var id = int.Parse(query[nameof(Product.ProductId)] + string.Empty);
																								var quantity = int.Parse(query[nameof(CartItem.Quantity)] + string.Empty);
																								var ps = sp.GetRequiredService<IProductService>();
																								var products = await ps.GetAll(default);
																								var p = products.FirstOrDefault(p => p.ProductId == id);
																								return new CartItem(p!, (uint)quantity);
																							})),
					new ViewMap(View: typeof(CheckoutPage)),
					forgotPasswordDialog
					);


			routes
				.Register(
					new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
							Nested: new RouteMap[]
							{
								new RouteMap("Login", View: views.FindByResultData<Credentials>(),
										Nested: new RouteMap[]
										{
											new ("Forgot", forgotPasswordDialog)
										}),
								new RouteMap("Home", View: views.FindByData<Credentials>(),
										Nested: new RouteMap[]{
											new RouteMap("Products",
													View: views.FindByViewModel<ProductsViewModel>(),
													IsDefault: true,
													Nested: new  RouteMap[]{
														new RouteMap("Filter",  View: views.FindByViewModel<FiltersViewModel>())
													}),
											new RouteMap("Product",
													View: views.FindByViewModel<ProductDetailsViewModel>(),
													DependsOn:"Products"),

											new RouteMap("Deals", View: views.FindByViewModel<DealsViewModel>()),

											new RouteMap("Profile", View: views.FindByViewModel<ProfileViewModel>()),

											new RouteMap("Cart", View: views.FindByViewModel<CartViewModel>(),
													Nested: new []{
														new RouteMap("CartDetails",View: views.FindByViewModel<CartProductDetailsViewModel>()),
														new RouteMap("Checkout", View: views.FindByView<CheckoutPage>())
													})
											})
							}));

			;
		}
	}
}
