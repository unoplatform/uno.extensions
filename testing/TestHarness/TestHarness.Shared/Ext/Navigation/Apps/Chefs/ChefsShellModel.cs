namespace TestHarness.Ext.Navigation.Apps.Chefs;

internal record ChefsShellModel
{
	public INavigator Navigator { get; init; }

	public ChefsShellModel(INavigator navigator)
	{
		Navigator = navigator;

		_ = Start();
	}

	public async Task Start()
	{
		await Navigator.NavigateViewModelAsync<ChefsRootModel>(this, Qualifiers.ClearBackStack);
	}

}
