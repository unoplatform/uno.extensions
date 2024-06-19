namespace TestHarness.Ext.Navigation.TabBar;

[ReactiveBindable(false)]
public partial class TabBarHomeViewModel : ObservableObject
{
	private RouteResolverDefault routeResolver;
	public TabBarHomeViewModel(IServiceProvider services)
	{
		routeResolver = services.GetRequiredService<RouteResolverDefault>();
		ImplicitMappingEnabled = routeResolver.ReturnImplicitMapping;
	}

	[ObservableProperty]
	private bool _implicitMappingEnabled;

	[RelayCommand]
	public void ToggleIsChecked()
	{
		routeResolver.ReturnImplicitMapping = !routeResolver.ReturnImplicitMapping;
		ImplicitMappingEnabled = routeResolver.ReturnImplicitMapping;
	}
}

