namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationTenViewModel (INavigator Navigator, IDispatcher Dispatcher, IWritableOptions<PageNavigationSettings> Settings)
	: BasePageNavigationViewModel(Dispatcher)
{


}
