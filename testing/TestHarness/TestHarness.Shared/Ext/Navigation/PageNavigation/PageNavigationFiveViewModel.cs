namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationFiveViewModel(INavigator Navigator, IDispatcher Dispatcher, IWritableOptions<PageNavigationSettings> Settings)
	: BasePageNavigationViewModel(Dispatcher)
{
	public async void GoBack()
	{
		await Settings.UpdateAsync(s => s with { PagesVisited = s.PagesVisited.Add(this.GetType().Name) });
		await Navigator.GoBack(this);
	}

	public async void GoToSixWithData()
	{
		await Settings.UpdateAsync(s => s with { PagesVisited = s.PagesVisited.Add(this.GetType().Name) });
		await Navigator.NavigateViewModelAsync<PageNavigationSixViewModel>(this, data: new Widget { Name="Bob"});
	}

	public async void GoToSixWithDataAndBack()
	{
		await Settings.UpdateAsync(s => s with { PagesVisited = s.PagesVisited.Add(this.GetType().Name) });
		await Navigator.NavigateViewModelAsync<PageNavigationSixViewModel>(this, data: new Widget { Name = "Bob" }, qualifier:Qualifiers.NavigateBack);
	}
}
