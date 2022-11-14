namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationFiveViewModel(INavigator Navigator, IDispatcher Dispatcher, IWritableOptions<PageNavigationSettings> Settings)
	: BasePageNavigationViewModel(Dispatcher)
{
	public async void GoBack()
	{
		await Settings.UpdateAsync(s => s with { PagesVisited = s.PagesVisited.Add(this.GetType().Name) });
		await Navigator.GoBack(this);
	}
}
