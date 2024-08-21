namespace TestHarness.Ext.Navigation.RoutesNavigation;

[TestSectionRoot("Routes Navigation", TestSections.Navigation_RoutesNavigation, typeof(RoutesNavigationHostInit))]
[TestSectionRoot("Routes Navigation - Registerd Routes", TestSections.Navigation_RoutesNavigationRegistered, typeof(RoutesNavigationRegisterHostInit))]
public sealed partial class RoutesNavigationMainPage : BaseTestSectionPage
{
	public RoutesNavigationMainPage()
	{
		this.InitializeComponent();
	}

}
