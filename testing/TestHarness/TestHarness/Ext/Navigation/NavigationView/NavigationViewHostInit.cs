namespace TestHarness.Ext.Navigation.NavigationView;

public class NavigationViewHostInit : BaseHostInitialization
{
	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return builder.ConfigureServices(services => services.AddSingleton<INavigationViewDataService, NavigationViewDataService>());
	}

	protected override IHostBuilder Navigation(IHostBuilder builder)
	{
		return builder.UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes);
	}

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
			new ViewMap<NavigationViewHomePage, NavigationViewHomeViewModel>(),
			new ViewMap<NavigationViewDataBoundPage, NavigationViewDataBoundViewModel>(),
			new ViewMap<NavigationViewSettingsPage, NavigationViewSettingsViewModel>(),
			new ViewMap<NavigationViewDataPage, NavigationViewDataViewModel>(),
			new ViewMap<NavigationViewDataRecipesPage, NavigationViewDataRecipesViewModel>(),
			new DataViewMap<NavigationViewDataRecipeDetailsPage, NavigationViewDataRecipeDetailsViewModel, Recipe>(),
			new ViewMap<NavigationViewDataCookbooksPage, NavigationViewDataCookbooksViewModel>(),
			new DataViewMap<NavigationViewDataCookbookDetailsPage, NavigationViewDataCookbookDetailsViewModel, CookBook>(),
			new ViewMap<NavigationViewDataEntityPickerFlyout, NavigationViewDataEntityPickerViewModel>()
			);


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap("",
				Nested: new[]
				{
						new RouteMap("Home", View: views.FindByViewModel<NavigationViewHomeViewModel>()),
						new RouteMap("DataBound", View: views.FindByViewModel<NavigationViewDataBoundViewModel>(),
						Nested: new[]
						{
							new RouteMap("Profile"),
							new RouteMap("Deals"),
							new RouteMap("Products")
						}),
						new RouteMap("Data", View: views.FindByViewModel<NavigationViewDataViewModel>(),
						Nested: new[]
						{
							new RouteMap("Recipes", View: views.FindByViewModel<NavigationViewDataRecipesViewModel>(), IsDefault:true),
							new RouteMap("RecipeDetails", View: views.FindByViewModel<NavigationViewDataRecipeDetailsViewModel>(), DependsOn:"Recipes"),
							new RouteMap("Cookbooks", View: views.FindByViewModel<NavigationViewDataCookbooksViewModel>()),
							new RouteMap("CookbookDetails", View: views.FindByViewModel<NavigationViewDataCookbookDetailsViewModel>(), DependsOn:"Cookbooks")
						}),
						new RouteMap("Settings", View: views.FindByViewModel<NavigationViewSettingsViewModel>()),
						new RouteMap("EntityPicker", View: views.FindByViewModel<NavigationViewDataEntityPickerViewModel>())
				}));
	}
}


