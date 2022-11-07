namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationSevenViewModel (INavigator Navigator, IDispatcher Dispatcher, IWritableOptions<PageNavigationSettings> Settings)
	: BasePageNavigationViewModel(Dispatcher)
{

}
