namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationSixViewModel (INavigator Navigator, IDispatcher Dispatcher, IWritableOptions<PageNavigationSettings> Settings)
	: BasePageNavigationViewModel(Dispatcher)
{

}
