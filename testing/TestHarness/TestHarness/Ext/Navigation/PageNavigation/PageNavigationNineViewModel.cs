namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationNineViewModel (INavigator Navigator, IDispatcher Dispatcher, IWritableOptions<PageNavigationSettings> Settings)
	: BasePageNavigationViewModel(Dispatcher)
{


}
