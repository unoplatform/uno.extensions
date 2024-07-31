namespace TestHarness.Ext.Navigation.Apps.Commerce;

public record CommerceProductsViewModel(INavigator Navigator) : BaseCommerceViewModel()
{
	public CommerceProduct[] Products { get; } = new[]
				{
					new CommerceProduct("Shoes"),
					new CommerceProduct("Hat"),
					new CommerceProduct("Sun glasses"),
					new CommerceProduct("Watch")
				};
	public async void ShowFirstProductUIThread()
	{
		await Navigator.NavigateDataAsync(this, Products.First());
	}
	public async void ShowFirstProductBackgroundThread()
	{
		await Task.Run(async () =>
		{
			await Navigator.NavigateDataAsync(this, Products.First());
		});
	}
}
