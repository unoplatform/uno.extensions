namespace TestHarness.Ext.Navigation.Apps.Commerce;

public record CommerceDealsViewModel(INavigator Navigator) : BaseCommerceViewModel()
{
	public CommerceProduct[] Deals { get; } = new[]
			{
					new CommerceProduct("Shoes (deal)"),
					new CommerceProduct("Hat (deal)"),
					new CommerceProduct("Sun glasses (deal)"),
					new CommerceProduct("Watch (deal)")
				};
	public async void ShowFirstDealUIThread()
	{
		await Navigator.NavigateDataAsync(this, Deals.First());
	}
	public async void ShowFirstDealBackgroundThread()
	{
		await Task.Run(async () =>
		{
			await Navigator.NavigateDataAsync(this, Deals.First());
		});
	}
	public async void ShowProduct()
	{
			await Navigator.NavigateRouteAsync(this, "Product",data: Deals.First());
	}
}
