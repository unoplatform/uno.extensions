namespace TestHarness.Ext.Navigation.AddressBar;

public class AddressBarHostInit : BaseHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Navigation.AddressBar.appsettings.addressbar.json" };

	public AddressBarHostInit()
	{
		if (ApplicationData.Current.LocalSettings.Values.TryGetValue(Constants.HomeInstanceCountKey, out var value))
		{
			ApplicationData.Current.LocalSettings.Values[Constants.HomeInstanceCountKey] = 0;
		}
	}
	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap(ViewModel: typeof(ShellViewModel)),
			new ViewMap<AddressBarHomePage, AddressBarHomeModel>(),
			new DataViewMap<AddressBarSecondPage, AddressBarSecondModel, AddressBarUser>(
				ToQuery: user => new Dictionary<string, string>
				{
					{ "QueryUser.Id", $"{user.UserId}" }
				},
				FromQuery: async (sp, query) =>
				{
					var userService = new AddressBarUserService();

					if (Guid.TryParse($"{query["QueryUser.Id"]}", out var guid))
					{
						var user = userService.GetById(guid);
						return user ?? new AddressBarUser(guid, "User not found");
					}

					return new AddressBarUser(guid, "User not found");
				}
			)
		);

		routes.Register(
			new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
				Nested:
				[
					new RouteMap("AddressBarHome", View: views.FindByViewModel<AddressBarHomeModel>(), IsDefault: true),
					new RouteMap("AddressBarSecond", View: views.FindByViewModel<AddressBarSecondModel>())
				]
			)
		);
	}
}
