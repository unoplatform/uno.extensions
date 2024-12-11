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
			new ViewMap<AddressBarHomePage, AddressBarHomeModel>(),
			new ViewMap(ViewModel: typeof(ShellViewModel))
		);

		routes.Register(
			new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
				Nested:
				[
					new RouteMap("AddressBarHome", View: views.FindByViewModel<AddressBarHomeModel>(), IsDefault: true)
				]
			)
		);
	}
}
