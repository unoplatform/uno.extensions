namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationFiveViewModel(INavigator Navigator, IDispatcher Dispatcher, IWritableOptions<PageNavigationSettings> Settings)
	: BasePageNavigationViewModel(Dispatcher)
{
	public async void GoBack()
	{
#if !__WASM__
		await Settings.UpdateAsync(s => s with { PagesVisited = s.PagesVisited.Add(this.GetType().Name) });
#endif
		await Navigator.GoBack(this);
	}
}
