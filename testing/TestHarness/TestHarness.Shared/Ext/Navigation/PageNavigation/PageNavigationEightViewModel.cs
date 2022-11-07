namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationEightViewModel (INavigator Navigator, IDispatcher Dispatcher, IWritableOptions<PageNavigationSettings> Settings)
	: BasePageNavigationViewModel(Dispatcher)
{
	
}
