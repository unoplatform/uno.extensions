namespace TestHarness.Ext.Navigation.Apps.Commerce;

public record CommerceHomeViewModel(INavigator Navigator)
{
	public async Task GoToProducts()
	{
		await Task.Run(async () =>
		await Navigator.NavigateViewModelAsync<CommerceProductsViewModel>(this, qualifier: Qualifiers.Nested)
		);
	}

}
