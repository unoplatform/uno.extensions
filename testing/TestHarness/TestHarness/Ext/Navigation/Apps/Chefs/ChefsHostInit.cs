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
			new ViewMap<ChefsFilterPage, ChefsFilterModel>(Data: new DataMap<ChefsSearchFilter>()),
			new ViewMap<ChefsHomePage, ChefsHomeModel>(),
			new DataViewMap<ChefsCreateUpdateCookbookPage, ChefsCreateUpdateCookbookModel, ChefsCookbook>(),
			new ViewMap<ChefsLoginPage, ChefsLoginModel>(ResultData: typeof(ChefsCredentials)),
			new ViewMap<ChefsNotificationsPage, ChefsNotificationsModel>(),
			new ViewMap<ChefsProfilePage, ChefsProfileModel>(Data: new DataMap<ChefsUser>(), ResultData: typeof(ChefsIChefEntity)),
			new ViewMap<ChefsRecipeDetailsPage, ChefsRecipeDetailsModel>(Data: new DataMap<ChefsRecipe>()),
			new ViewMap<ChefsFavoriteRecipesPage, ChefsFavoriteRecipesModel>(),
			new DataViewMap<ChefsSearchPage, ChefsSearchModel, ChefsSearchFilter>(),
			new ViewMap<ChefsSettingsPage, ChefsSettingsModel>(Data: new DataMap<ChefsUser>()),
			new ViewMap<ChefsLiveCookingPage, ChefsLiveCookingModel>(Data: new DataMap<ChefsLiveCookingParameter>()),
			new ViewMap<ChefsReviewsPage, ChefsReviewsModel>(Data: new DataMap<ChefsReviewParameter>()),
			new ViewMap<ChefsCookbookDetailPage, ChefsCookbookDetailModel>(Data: new DataMap<ChefsCookbook>()),
			new ViewMap<ChefsCompletedDialog>(),
			new ViewMap<ChefsMapPage, ChefsMapModel>(),
			new ViewMap<ChefsGenericDialog, ChefsGenericDialogModel>(Data: new DataMap<ChefsDialogInfo>())
		);

		routes.Register(
			new RouteMap("", View: views.FindByViewModel<ChefsShellModel>(),
				Nested: new RouteMap[]
				{
					new RouteMap("ChefsWelcome", View: views.FindByViewModel<ChefsWelcomeModel>()),
					new RouteMap("ChefsLogin", View: views.FindByViewModel<ChefsLoginModel>()),
					new RouteMap("ChefsRoot", View: views.FindByViewModel<ChefsRootModel>(), Nested: new RouteMap[]
					{
						#region Main Tabs
						new RouteMap("ChefsHome", View: views.FindByViewModel<ChefsHomeModel>(), IsDefault: true),
						new RouteMap("ChefsSearch", View: views.FindByViewModel<ChefsSearchModel>()),
						new RouteMap("ChefsFavoriteRecipes", View: views.FindByViewModel<ChefsFavoriteRecipesModel>(), Nested: new[]
						{
							new RouteMap("ChefsMyRecipes"),
							new RouteMap("ChefsCookbooks")
						}),
						#endregion

						#region Cookbooks
						new RouteMap("ChefsCookbookDetails", View: views.FindByViewModel<ChefsCookbookDetailModel>(), DependsOn: "FavoriteRecipes"),
						new RouteMap("ChefsUpdateCookbook", View: views.FindByViewModel<ChefsCreateUpdateCookbookModel>(), DependsOn: "FavoriteRecipes"),
						new RouteMap("ChefsCreateCookbook", View: views.FindByViewModel<ChefsCreateUpdateCookbookModel>(), DependsOn: "FavoriteRecipes"),
						#endregion

						#region Recipe Details
						new RouteMap("ChefsRecipeDetails", View: views.FindByViewModel<ChefsRecipeDetailsModel>(), DependsOn: "Home", Nested: new[] {
							new RouteMap("ChefsIngredientsTabWide"),
							new RouteMap("ChefsStepsTabWide"),
							new RouteMap("ChefsReviewsTabWide"),
							new RouteMap("ChefsNutritionTabWide"),
							new RouteMap("ChefsIngredientsTab"),
							new RouteMap("ChefsStepsTab"),
							new RouteMap("ChefsReviewsTab"),
							new RouteMap("ChefsNutritionTab"),
						}),
						new RouteMap("ChefsSearchRecipeDetails", View: views.FindByViewModel<ChefsRecipeDetailsModel>(), DependsOn: "ChefsSearch"),
						new RouteMap("ChefsFavoriteRecipeDetails", View: views.FindByViewModel<ChefsRecipeDetailsModel>(), DependsOn: "ChefsFavoriteRecipes"),
						new RouteMap("ChefsCookbookRecipeDetails", View: views.FindByViewModel<ChefsRecipeDetailsModel>(), DependsOn: "ChefsFavoriteRecipes"),
						#endregion

						#region Live Cooking
						new RouteMap("ChefsLiveCooking", View: views.FindByViewModel<ChefsLiveCookingModel>(), DependsOn: "ChefsRecipeDetails"),
						new RouteMap("ChefsSearchLiveCooking", View: views.FindByViewModel<ChefsLiveCookingModel>(), DependsOn: "ChefsSearchRecipeDetails"),
						new RouteMap("ChefsFavoriteLiveCooking", View: views.FindByViewModel<ChefsLiveCookingModel>(), DependsOn: "ChefsFavoriteRecipeDetails"),
						new RouteMap("ChefsCookbookLiveCooking", View: views.FindByViewModel<ChefsLiveCookingModel>(), DependsOn: "ChefsCookbookRecipeDetails"),
						#endregion

						new RouteMap("ChefsMap", View: views.FindByViewModel<ChefsMapModel>(), DependsOn: "ChefsHome"),
					}),
					new RouteMap("ChefsNotifications", View: views.FindByViewModel<ChefsNotificationsModel>(), Nested: new RouteMap[]
					{
						new RouteMap("ChefsAllTab"),
						new RouteMap("ChefsUnreadTab"),
						new RouteMap("ChefsReadTab"),
					}),
					new RouteMap("ChefsFilter", View: views.FindByViewModel<ChefsFilterModel>()),
					new RouteMap("ChefsReviews", View: views.FindByViewModel<ChefsReviewsModel>()),
					new RouteMap("ChefsProfile", View: views.FindByViewModel<ChefsProfileModel>()),
					new RouteMap("ChefsSettings", View: views.FindByViewModel<ChefsSettingsModel>(), DependsOn: "ChefsProfile"),
					new RouteMap("ChefsCompleted", View: views.FindByView<ChefsCompletedDialog>()),
					new RouteMap("ChefsMap", View: views.FindByViewModel<ChefsMapModel>(), DependsOn: "ChefsMain"),
					new RouteMap("ChefsDialog", View: views.FindByView<ChefsGenericDialog>())
				}
			)
		);
	}
}


