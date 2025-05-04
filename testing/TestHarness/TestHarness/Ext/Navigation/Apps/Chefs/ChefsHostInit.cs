using TestHarness.Ext.Navigation.Apps.Chefs.Models;

namespace TestHarness.Ext.Navigation.Apps.Chefs;

public partial class ChefsHostInit : BaseHostInitialization
{
	protected override IHostBuilder Navigation(IHostBuilder builder)
	{
		return builder.UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes).UseToolkitNavigation();
	}
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
		new ViewMap(ViewModel: typeof(ChefsShellModel)),
		new ViewMap<ChefsRootPage, ChefsRootModel>(),
		new ViewMap<ChefsWelcomePage, ChefsWelcomeModel>(),
		new DataViewMap<ChefsFilterPage, ChefsFilterModel, ChefsSearchFilter>(),
		new ViewMap<ChefsHomePage, ChefsHomeModel>(),
		new DataViewMap<ChefsCreateUpdateCookbookPage, ChefsCreateUpdateCookbookModel, ChefsCookbook>(),
		new ViewMap<ChefsLoginPage, ChefsLoginModel>(ResultData: typeof(ChefsCredentials)),
		new ViewMap<ChefsRegistrationPage, ChefsRegistrationModel>(),
		new ViewMap<ChefsNotificationsPage, ChefsNotificationsModel>(),
		new ViewMap<ChefsProfilePage, ChefsProfileModel>(Data: new DataMap<ChefsUser>(), ResultData: typeof(ChefsIChefEntity)),
		new ViewMap<ChefsRecipeDetailsPage, ChefsRecipeDetailsModel>(Data: new DataMap<ChefsRecipe>()),
		new ViewMap<ChefsFavoriteRecipesPage, ChefsFavoriteRecipesModel>(),
		new DataViewMap<ChefsSearchPage, ChefsSearchModel, ChefsSearchFilter>(),
		new ViewMap<ChefsSettingsPage, ChefsSettingsModel>(Data: new DataMap<ChefsUser>()),
		new ViewMap<ChefsLiveCookingPage, ChefsLiveCookingModel>(Data: new DataMap<ChefsLiveCookingParameter>()),
		new ViewMap<ChefsCookbookDetailPage, ChefsCookbookDetailModel>(Data: new DataMap<ChefsCookbook>()),
		new ViewMap<ChefsCompletedDialog>(),
		new ViewMap<ChefsMapPage, ChefsMapModel>(),
		new ViewMap<ChefsGenericDialog, ChefsGenericDialogModel>(Data: new DataMap<ChefsDialogInfo>())
	);

	routes.Register(
		new RouteMap("", View: views.FindByViewModel<ChefsShellModel>(),
			Nested:
			[
				new RouteMap("ChefsWelcome", View: views.FindByViewModel<ChefsWelcomeModel>()),
				new RouteMap("ChefsLogin", View: views.FindByViewModel<ChefsLoginModel>()),
				new RouteMap("ChefsRegister", View: views.FindByViewModel<ChefsRegistrationModel>()),
				new RouteMap("ChefsRoot", View: views.FindByViewModel<ChefsRootModel>(), Nested:
				[
					#region Main Tabs
					new RouteMap("ChefsHome", View: views.FindByViewModel<ChefsHomeModel>(), IsDefault: true),
					new RouteMap("ChefsSearch", View: views.FindByViewModel<ChefsSearchModel>()),
					new RouteMap("ChefsFavoriteRecipes", View: views.FindByViewModel<ChefsFavoriteRecipesModel>()),
					#endregion

					#region Cookbooks
					new RouteMap("ChefsCookbookDetails", View: views.FindByViewModel<ChefsCookbookDetailModel>()),
					new RouteMap("ChefsUpdateCookbook", View: views.FindByViewModel<ChefsCreateUpdateCookbookModel>()),
					new RouteMap("ChefsCreateCookbook", View: views.FindByViewModel<ChefsCreateUpdateCookbookModel>()),
					#endregion

					#region Recipe Details
					new RouteMap("ChefsRecipeDetails", View: views.FindByViewModel<ChefsRecipeDetailsModel>()),
					#endregion

					#region Live Cooking
					new RouteMap("ChefsLiveCooking", View: views.FindByViewModel<ChefsLiveCookingModel>()),
					#endregion

					new RouteMap("ChefsMap", View: views.FindByViewModel<ChefsMapModel>()),

				]),
				new RouteMap("ChefsNotifications", View: views.FindByViewModel<ChefsNotificationsModel>()),
				new RouteMap("ChefsFilter", View: views.FindByViewModel<ChefsFilterModel>()),
				new RouteMap("ChefsProfile", View: views.FindByViewModel<ChefsProfileModel>()),
				new RouteMap("ChefsSettings", View: views.FindByViewModel<ChefsSettingsModel>()),
				new RouteMap("ChefsCompleted", View: views.FindByView<ChefsCompletedDialog>()),
				new RouteMap("ChefsMap", View: views.FindByViewModel<ChefsMapModel>()),
				new RouteMap("ChefsDialog", View: views.FindByView<ChefsGenericDialog>())
			]
		)
	);
	}
}


