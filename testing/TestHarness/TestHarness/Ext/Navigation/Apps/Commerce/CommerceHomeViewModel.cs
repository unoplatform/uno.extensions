namespace TestHarness.Ext.Navigation.Apps.Commerce;

public record CommerceHomeViewModel(ILogger<CommerceHomeViewModel> Logger, INavigator Navigator)
{
	public async void GoToProducts()
	{
		Logger.LogInformationMessage("Go to products");
		await Task.Run(async () =>
		await Navigator.NavigateViewModelAsync<CommerceProductsViewModel>(this, qualifier: Qualifiers.Nested)
		);
	}

	public async void GoToProductsClearStack()
	{
		Logger.LogInformationMessage("Go to products - clear stack");
		await Task.Run(async () =>
		await Navigator.NavigateViewModelAsync<CommerceProductsViewModel>(this, qualifier: Qualifiers.Nested + Qualifiers.ClearBackStack)
		);
	}
}
